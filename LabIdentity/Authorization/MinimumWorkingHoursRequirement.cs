using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace LabIdentity.Authorization;

public sealed class MinimumWorkingHoursRequirement(decimal minimumHours)
    : IAuthorizationRequirement
{
    public decimal MinimumHours { get; } = minimumHours;
}

public sealed class MinimumWorkingHoursHandler
    : AuthorizationHandler<MinimumWorkingHoursRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumWorkingHoursRequirement requirement)
    {
        var value = context.User.FindFirst("WorkingHours")?.Value;

        if (decimal.TryParse(
                value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var hours)
            && hours >= requirement.MinimumHours)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}