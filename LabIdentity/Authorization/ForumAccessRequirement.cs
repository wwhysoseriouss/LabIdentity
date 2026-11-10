using Microsoft.AspNetCore.Authorization;

namespace LabIdentity.Authorization;

public sealed class ForumAccessRequirement : IAuthorizationRequirement
{
}

public sealed class ForumAccessHandler
    : AuthorizationHandler<ForumAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ForumAccessRequirement requirement)
    {
        if (context.User.HasClaim("IsMentor", "true")
            || context.User.HasClaim("IsVerifiedUser", "true")
            || context.User.HasClaim("HasForumAccess", "true"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}