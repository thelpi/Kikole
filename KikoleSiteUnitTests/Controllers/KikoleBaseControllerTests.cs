using System.Collections.Generic;
using System.Security.Claims;
using KikoleSite;
using KikoleSite.Controllers;
using KikoleSite.Identity;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Controllers;

/// <summary>
/// Sous-classe minimale exposant les membres protected de <see cref="KikoleBaseController"/>
/// que chaque contrôleur du projet hérite : c'est de la base d'autorisation et de routing
/// (le suffixe "submit-X" des boutons), jamais exercée par un test jusqu'ici faute de
/// contrôleur concret instancié en isolation.
/// </summary>
internal sealed class TestableController : KikoleBaseController
{
    public TestableController(
        IUserRepository userRepository,
        IInternationalService internationalService,
        IClock clock,
        IGameCalendar gameCalendar,
        IPlayerService playerService,
        IBadgeService badgeService,
        IHttpContextAccessor httpContextAccessor)
        : base(userRepository, internationalService, clock, gameCalendar, playerService, badgeService, httpContextAccessor)
    {
    }

    internal ulong ExposedUserId => UserId;
    internal string? ExposedUserLogin => UserLogin;
    internal UserTypes ExposedUserType => UserType;
    internal string? ExposedGetSubmitAction() => GetSubmitAction();
    internal bool ExposedIsTypeOfUser(UserTypes minimalType) => IsTypeOfUser(minimalType);
}

public class KikoleBaseControllerTests
{
    private readonly DefaultHttpContext _httpContext = new();
    private readonly TestableController _controller;

    public KikoleBaseControllerTests()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);

        _controller = new TestableController(
            new Mock<IUserRepository>().Object,
            new Mock<IInternationalService>().Object,
            new Mock<IClock>().Object,
            new Mock<IGameCalendar>().Object,
            new Mock<IPlayerService>().Object,
            new Mock<IBadgeService>().Object,
            httpContextAccessor.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            }
        };
    }

    private void SetUser(ulong? userId, string? login, UserTypes? userType)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        if (userType.HasValue)
            claims.Add(new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)userType.Value).ToString()));

        var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null);
        if (login != null)
            identity.AddClaim(new Claim(ClaimTypes.Name, login));

        _httpContext.User = new ClaimsPrincipal(identity);
    }

    // ------------------------------------------------------------- UserId / UserLogin

    [Fact]
    public void UserId_WithAValidClaim_ParsesIt()
    {
        SetUser(userId: 42, login: null, userType: null);

        _controller.ExposedUserId.Should().Be(42);
    }

    [Fact]
    public void UserId_WithoutTheClaim_DefaultsToZero()
    {
        SetUser(userId: null, login: null, userType: null);

        _controller.ExposedUserId.Should().Be(0);
    }

    [Fact]
    public void UserLogin_ReflectsTheIdentityName()
    {
        SetUser(userId: 1, login: "joueur1", userType: null);

        _controller.ExposedUserLogin.Should().Be("joueur1");
    }

    // ------------------------------------------------------------- UserType / IsTypeOfUser

    [Theory]
    [InlineData(UserTypes.StandardUser, UserTypes.StandardUser, true)]
    [InlineData(UserTypes.PowerUser, UserTypes.StandardUser, true)]
    [InlineData(UserTypes.Administrator, UserTypes.StandardUser, true)]
    [InlineData(UserTypes.StandardUser, UserTypes.PowerUser, false)]
    [InlineData(UserTypes.PowerUser, UserTypes.Administrator, false)]
    [InlineData(UserTypes.Administrator, UserTypes.Administrator, true)]
    public void IsTypeOfUser_ComparesTheTierNumerically(UserTypes actual, UserTypes minimal, bool expected)
    {
        SetUser(userId: 1, login: "x", userType: actual);

        _controller.ExposedIsTypeOfUser(minimal).Should().Be(expected);
    }

    [Fact]
    public void UserType_WithoutTheClaim_FallsBackToTheLeastPrivilegedProfile()
    {
        SetUser(userId: 1, login: "x", userType: null);

        _controller.ExposedUserType.Should().Be(UserTypes.StandardUser);
    }

    [Fact]
    public void UserType_WithAnUnparseableClaim_FallsBackToTheLeastPrivilegedProfile()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, "pas-un-nombre")
        }, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        _controller.ExposedUserType.Should().Be(UserTypes.StandardUser);
    }

    // ------------------------------------------------------------- GetSubmitAction

    private void SetFormKeys(params string[] keys)
    {
        var dict = new Dictionary<string, StringValues>();
        foreach (var key in keys)
            dict[key] = StringValues.Empty;
        _httpContext.Request.Form = new FormCollection(dict);
    }

    [Fact]
    public void GetSubmitAction_ExtractsTheSuffixOfTheSingleSubmitKey()
    {
        SetFormKeys("submit-Country", "CountryNameSubmission", "CurrentDay");

        _controller.ExposedGetSubmitAction().Should().Be("Country");
    }

    [Fact]
    public void GetSubmitAction_WithNoSubmitKey_ReturnsNull()
    {
        SetFormKeys("CurrentDay");

        _controller.ExposedGetSubmitAction().Should().BeNull();
    }

    [Fact]
    public void GetSubmitAction_WithSeveralSubmitKeys_ReturnsNull()
    {
        // ne devrait pas arriver depuis un vrai formulaire (un seul bouton peut etre
        // actionne a la fois), mais la methode doit rester defensive
        SetFormKeys("submit-Country", "submit-Continent");

        _controller.ExposedGetSubmitAction().Should().BeNull();
    }

    [Fact]
    public void GetSubmitAction_WithAMalformedKey_ReturnsNull()
    {
        SetFormKeys("submit-Country-Extra");

        _controller.ExposedGetSubmitAction().Should().BeNull();
    }
}
