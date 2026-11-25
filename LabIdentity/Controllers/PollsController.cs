using LabData.Data;
using LabData.Models;
using LabIdentity.Configuration;
using LabIdentity.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabIdentity.Controllers;

[Authorize]
public class PollsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly LabSettings _settings;

    public PollsController(
        ApplicationDbContext db,
        LabSettings settings)
    {
        _db = db;
        _settings = settings;
    }

    // Список голосувань і пошук за питанням.
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

    // Перегляд одного голосування.
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

    // Обробка заповненої форми.
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
            TempData["Error"] =
                "Не можна редагувати голосування, яке вже має голоси.";

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
    [FromRoute] int id,
    PollFormViewModel model)
    {
        await using var transaction = await _db.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

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
            TempData["Error"] =
                "Не можна редагувати голосування, яке вже має голоси.";

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
                "Збільште тривалість: дата завершення повинна бути в майбутньому.");

            return View("Form", model);
        }

        poll.Question = model.Question.Trim();
        poll.Description = model.Description?.Trim();
        poll.ClosesAtUtc = closesAtUtc;

        // Прибираємо попередні варіанти.
        _db.PollOptions.RemoveRange(poll.Options.ToList());
        poll.Options.Clear();

        // Додаємо новий список варіантів.
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

        TempData["Success"] = "Зміни збережено.";

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

    // Безпосереднє видалення.
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        await using var transaction = await _db.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

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
            TempData["Error"] =
                "Не можна видалити голосування, яке вже має голоси.";

            return RedirectToAction(nameof(Details), new { id });
        }

        _db.Polls.Remove(poll);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = "Голосування видалено.";

        return RedirectToAction(nameof(Index));
    }

    // Розділення тексту на варіанти та перевірка правил.
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
                $"Вкажіть від 2 до {maxOptions} варіантів відповіді.");
        }

        if (options.Any(option => option.Length > 200))
        {
            ModelState.AddModelError(
                nameof(PollFormViewModel.OptionsText),
                "Кожен варіант повинен містити не більше 200 символів.");
        }

        var uniqueCount = options
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (uniqueCount != options.Length)
        {
            ModelState.AddModelError(
                nameof(PollFormViewModel.OptionsText),
                "Варіанти відповіді не повинні повторюватися.");
        }

        return options;
    }
}