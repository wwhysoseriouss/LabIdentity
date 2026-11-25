using System.Security.Claims;
using LabData.Data;
using LabData.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LabIdentity.Controllers;

[Authorize]
public class VotesController : Controller
{
    private readonly ApplicationDbContext _db;

    public VotesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cast(
        [FromForm] int pollId,
        [FromForm] int? pollOptionId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        try
        {
            await using var transaction = await _db.Database
                .BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            var poll = await _db.Polls
                .FirstOrDefaultAsync(poll => poll.Id == pollId);

            if (poll is null)
            {
                return NotFound();
            }

            var now = DateTimeOffset.UtcNow;

            if (now < poll.OpensAtUtc)
            {
                return VoteError(
                    pollId,
                    "Голосування ще не почалося.");
            }

            if (poll.ClosesAtUtc.HasValue
                && now >= poll.ClosesAtUtc.Value)
            {
                return VoteError(
                    pollId,
                    "Голосування вже завершено.");
            }

            if (!pollOptionId.HasValue)
            {
                return VoteError(
                    pollId,
                    "Виберіть варіант відповіді.");
            }

            var optionExists = await _db.PollOptions.AnyAsync(
                option => option.Id == pollOptionId.Value
                    && option.PollId == pollId);

            if (!optionExists)
            {
                return VoteError(
                    pollId,
                    "Вибраний варіант не належить цьому голосуванню.");
            }

            var alreadyVoted = await _db.Votes.AnyAsync(
                vote => vote.PollId == pollId
                    && vote.VoterId == userId);

            if (alreadyVoted)
            {
                return VoteError(
                    pollId,
                    "Ви вже проголосували в цьому голосуванні.");
            }

            var vote = new Vote
            {
                PollId = pollId,
                PollOptionId = pollOptionId.Value,
                VoterId = userId,
                CastAtUtc = now
            };

            _db.Votes.Add(vote);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "Ваш голос зараховано.";

            return RedirectToAction(
                "Details",
                "Polls",
                new { id = pollId });
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sql
                && (sql.Number is 2601 or 2627))
        {
            return VoteError(
                pollId,
                "Повторний голос не збережено. Оновіть сторінку.");
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sql
                && sql.Number == 1205)
        {
            return VoteError(
                pollId,
                "Дані одночасно змінювалися. Спробуйте ще раз.");
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return VoteError(
                pollId,
                "Дані одночасно змінювалися. Спробуйте ще раз.");
        }
    }

    private IActionResult VoteError(int pollId, string message)
    {
        TempData["Error"] = message;

        return RedirectToAction(
            "Details",
            "Polls",
            new { id = pollId });
    }
}