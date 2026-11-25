using LabData.Data;
using LabData.Models;
using LabIdentity.Configuration;
using LabIdentity.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace LabIdentity.Controllers;

[Authorize]
public class PollsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly LabSettings _settings;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PollsController(
        ApplicationDbContext db,
        LabSettings settings,
        IStringLocalizer<SharedResource> localizer)
    {
        _db = db;
        _settings = settings;
        _localizer = localizer;
    }

    // Список голосувань і пошук.
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? search)
    {
        var query = _db.Polls.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchText = search.Trim();

            query = query.Where(poll =>
                poll.Question.Contains(searchText));
        }

        var polls = await query
            .OrderByDescending(poll => poll.OpensAtUtc)
            .ThenByDescending(poll => poll.Id)
            .ToListAsync();

        ViewData["Search"] = search;

        return View(polls);
    }

    // Перегляд голосування.
    [HttpGet]
    public async Task<IActionResult> Details([FromRoute] int id)
    {
        var poll = await _db.Polls
            .AsNoTracking()
            .Include(poll => poll.Options)
            .Include(poll => poll.Votes)
            .FirstOrDefaultAsync(poll => poll.Id == id);

        if (poll is null)
        {
            return NotFound();
        }

        return View(poll);
    }

    // Відкриття форми створення.
    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new PollFormViewModel());
    }

    // Збереження нового голосування.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PollFormViewModel model)
    {
        var options = ParseAndValidateOptions(model.OptionsText);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var now = DateTimeOffset.UtcNow;

        var poll = new Poll
        {
            Question = model.Question.Trim(),
            Description = model.Description?.Trim(),
            OpensAtUtc = now,
            ClosesAtUtc = now.AddHours(model.DurationHours!.Value),

            Options = options.Select((text, index) => new PollOption
            {
                Text = text,
                DisplayOrder = index + 1
            }).ToList()
        };

        _db.Polls.Add(poll);

        await _db.SaveChangesAsync();

        return RedirectToAction(
            nameof(Details),
            new { id = poll.Id });
    }

    // Відкриття форми редагування.
    [HttpGet]
    public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var poll = await _db.Polls
            .AsNoTracking()
            .Include(poll => poll.Options)
            .FirstOrDefaultAsync(poll => poll.Id == id);

        if (poll is null)
        {
            return NotFound();
        }

        var hasVotes = await _db.Votes
            .AnyAsync(vote => vote.PollId == id);

        if (hasVotes)
        {
            TempData["Error"] = _localizer["PollEditBlocked"].Value;

            return RedirectToAction(nameof(Details), new { id });
        }

        var durationHours = poll.ClosesAtUtc.HasValue
            ? (int)Math.Ceiling(
                (poll.ClosesAtUtc.Value - poll.OpensAtUtc).TotalHours)
            : 24;

        var model = new PollFormViewModel
        {
            Question = poll.Question,
            Description = poll.Description,
            DurationHours = durationHours,
            OptionsText = string.Join(
                Environment.NewLine,
                poll.Options
                    .OrderBy(option => option.DisplayOrder)
                    .Select(option => option.Text))
        };

        return View("Form", model);
    }

    // Збереження змін.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [FromRoute] int id,
        PollFormViewModel model)
    {
        await using var transaction = await _db.Database
            .BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var poll = await _db.Polls
            .Include(poll => poll.Options)
            .FirstOrDefaultAsync(poll => poll.Id == id);

        if (poll is null)
        {
            return NotFound();
        }

        var hasVotes = await _db.Votes
            .AnyAsync(vote => vote.PollId == id);

        if (hasVotes)
        {
            TempData["Error"] = _localizer["PollEditBlocked"].Value;

            return RedirectToAction(nameof(Details), new { id });
        }

        var options = ParseAndValidateOptions(model.OptionsText);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var closesAtUtc = poll.OpensAtUtc
            .AddHours(model.DurationHours!.Value);

        if (closesAtUtc <= DateTimeOffset.UtcNow)
        {
            ModelState.AddModelError(
                nameof(model.DurationHours),
                _localizer["PollEndMustBeFuture"].Value);

            return View("Form", model);
        }

        poll.Question = model.Question.Trim();
        poll.Description = model.Description?.Trim();
        poll.ClosesAtUtc = closesAtUtc;

        // Видалення попередніх варіантів.
        _db.PollOptions.RemoveRange(poll.Options.ToList());
        poll.Options.Clear();

        // Створення нового списку варіантів.
        foreach (var item in options.Select((text, index) => new
        {
            Text = text,
            Order = index + 1
        }))
        {
            poll.Options.Add(new PollOption
            {
                Text = item.Text,
                DisplayOrder = item.Order
            });
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = _localizer["PollSaved"].Value;

        return RedirectToAction(nameof(Details), new { id });
    }

    // Сторінка підтвердження видалення.
    [HttpGet]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var poll = await _db.Polls
            .AsNoTracking()
            .Include(poll => poll.Options)
            .Include(poll => poll.Votes)
            .FirstOrDefaultAsync(poll => poll.Id == id);

        if (poll is null)
        {
            return NotFound();
        }

        return View(poll);
    }

    // Видалення голосування.
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        await using var transaction = await _db.Database
            .BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var poll = await _db.Polls
            .FirstOrDefaultAsync(poll => poll.Id == id);

        if (poll is null)
        {
            return NotFound();
        }

        var hasVotes = await _db.Votes
            .AnyAsync(vote => vote.PollId == id);

        if (hasVotes)
        {
            TempData["Error"] = _localizer["PollDeleteBlocked"].Value;

            return RedirectToAction(nameof(Details), new { id });
        }

        _db.Polls.Remove(poll);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = _localizer["PollDeleted"].Value;

        return RedirectToAction(nameof(Index));
    }

    // Спільні перевірки варіантів відповіді.
    private string[] ParseAndValidateOptions(string? optionsText)
    {
        var options = (optionsText ?? string.Empty)
            .Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries);

        var maxOptions = _settings.Polling.MaxOptionsPerPoll;

        if (options.Length < 2 || options.Length > maxOptions)
        {
            ModelState.AddModelError(
                nameof(PollFormViewModel.OptionsText),
                _localizer["PollOptionsCountError", maxOptions].Value);
        }

        if (options.Any(option => option.Length > 200))
        {
            ModelState.AddModelError(
                nameof(PollFormViewModel.OptionsText),
                _localizer["PollOptionLengthError"].Value);
        }

        var uniqueCount = options
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (uniqueCount != options.Length)
        {
            ModelState.AddModelError(
                nameof(PollFormViewModel.OptionsText),
                _localizer["PollOptionsDuplicateError"].Value);
        }

        return options;
    }
}