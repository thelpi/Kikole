using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace KikoleSite.Identity;

public class MinimumUserTypeHandler : AuthorizationHandler<MinimumUserTypeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, MinimumUserTypeRequirement requirement)
    {
        var claim = context.User.FindFirst(UserTypeClaimsPrincipalFactory.UserTypeClaimType);

        if (claim != null
            && ulong.TryParse(claim.Value, out var userType)
            && userType >= (ulong)requirement.MinimalType)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
