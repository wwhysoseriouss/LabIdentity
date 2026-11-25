using System.Security.Claims;
using LabData.Data;
using LabData.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace LabIdentity.Controllers;

[Authorize]
public class VotesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public VotesController(
        ApplicationDbContext db,
        IStringLocalizer<SharedResource> localizer)
    {
        _db = db;
        _localizer = localizer;
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
                return VoteError(pollId, "VoteNotStarted");
            }

            if (poll.ClosesAtUtc.HasValue
                && now >= poll.ClosesAtUtc.Value)
            {
                return VoteError(pollId, "VoteFinished");
            }

            if (!pollOptionId.HasValue)
            {
                return VoteError(pollId, "VoteChooseOption");
            }

            var optionExists = await _db.PollOptions.AnyAsync(
                option => option.Id == pollOptionId.Value
                    && option.PollId == pollId);

            if (!optionExists)
            {
                return VoteError(pollId, "VoteInvalidOption");
            }

            var alreadyVoted = await _db.Votes.AnyAsync(
                vote => vote.PollId == pollId
                    && vote.VoterId == userId);

            if (alreadyVoted)
            {
                return VoteError(pollId, "AlreadyVotedInfo");
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

            TempData["Success"] = _localizer["VoteSaved"].Value;

            return RedirectToAction(
                "Details",
                "Polls",
                new { id = pollId });
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sql
                && (sql.Number is 2601 or 2627))
        {
            return VoteError(pollId, "VoteDuplicate");
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sql
                && sql.Number == 1205)
        {
            return VoteError(pollId, "VoteConflict");
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return VoteError(pollId, "VoteConflict");
        }
    }

    private IActionResult VoteError(int pollId, string resourceKey)
    {
        TempData["Error"] = _localizer[resourceKey].Value;

        return RedirectToAction(
            "Details",
            "Polls",
            new { id = pollId });
    }
}