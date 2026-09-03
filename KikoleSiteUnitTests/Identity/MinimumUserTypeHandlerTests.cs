using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Identity;
using KikoleSite.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace KikoleSiteUnitTests.Identity;

/// <summary>
/// Reproduit la semantique de l'ancien <c>IsTypeOfUser</c> : « au moins ce palier »,
/// compare numeriquement sur la claim <see cref="UserTypeClaimsPrincipalFactory.UserTypeClaimType"/>.
/// </summary>
public class MinimumUserTypeHandlerTests
{
    private static async Task<bool> SucceedsAsync(UserTypes? actualType, UserTypes minimalType)
    {
        var claims = actualType.HasValue
            ? new[] { new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)actualType.Value).ToString()) }
            : Array.Empty<Claim>();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var requirement = new MinimumUserTypeRequirement(minimalType);
        var context = new AuthorizationHandlerContext(new[] { requirement }, principal, null);

        await new MinimumUserTypeHandler().HandleAsync(context);

        return context.HasSucceeded;
    }

    [Theory]
    [InlineData(UserTypes.StandardUser, UserTypes.StandardUser, true)]
    [InlineData(UserTypes.PowerUser, UserTypes.StandardUser, true)]
    [InlineData(UserTypes.Administrator, UserTypes.StandardUser, true)]
    [InlineData(UserTypes.StandardUser, UserTypes.PowerUser, false)]
    [InlineData(UserTypes.PowerUser, UserTypes.PowerUser, true)]
    [InlineData(UserTypes.Administrator, UserTypes.PowerUser, true)]
    [InlineData(UserTypes.PowerUser, UserTypes.Administrator, false)]
    [InlineData(UserTypes.Administrator, UserTypes.Administrator, true)]
    public async Task HandleRequirementAsync_ComparesTheUserTypeNumerically(
        UserTypes actualType, UserTypes minimalType, bool expected)
    {
        (await SucceedsAsync(actualType, minimalType)).Should().Be(expected);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithoutTheClaim_Fails()
    {
        (await SucceedsAsync(null, UserTypes.StandardUser)).Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithAnUnparseableClaim_Fails()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, "not-a-number") }));
        var requirement = new MinimumUserTypeRequirement(UserTypes.StandardUser);
        var context = new AuthorizationHandlerContext(new[] { requirement }, principal, null);

        await new MinimumUserTypeHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
