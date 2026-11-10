using System.Security.Claims;
using LabData.Models;
using Microsoft.AspNetCore.Authorization;

namespace LabIdentity.Authorization;

public sealed class PollDraftAuthorRequirement : IAuthorizationRequirement
{
}

public sealed class PollDraftAuthorHandler
    : AuthorizationHandler<PollDraftAuthorRequirement, PollDraft>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PollDraftAuthorRequirement requirement,
        PollDraft draft)
    {
        var currentUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(currentUserId) && currentUserId == draft.AuthorId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}