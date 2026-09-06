using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KikoleSite;
using KikoleSite.Controllers;
using KikoleSite.Identity;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Controllers;

/// <summary>
/// Couvre <see cref="LeaderboardController"/>, jusqu'ici hors perimetre (cf. TODO).
/// Ecrit en filet de regression avant de paralleliser <c>UserDay</c> (dayboard +
/// proposals, une fois countryContinents et les gardes d'acces resolus) et les deux
/// <c>EnsureDateAsync</c> independants de <c>GetLeaderboardAsync</c>/<c>GetDailyboardAsync</c>.
/// </summary>
public class LeaderboardControllerTests
{
    private static readonly DateTime Today = TestCalendar.FirstDate.AddDays(30);

    private readonly DefaultHttpContext _httpContext = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IInternationalService> _internationalService = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly Mock<IPlayerService> _playerService = new();
    private readonly Mock<IBadgeService> _badgeService = new();
    private readonly Mock<ILeaderService> _leaderService = new();
    private readonly Mock<IProposalService> _proposalService = new();
    private readonly LeaderboardController _controller;

    public LeaderboardControllerTests()
    {
        _clock.Setup(_ => _.Today).Returns(Today);
        _clock.Setup(_ => _.Yesterday).Returns(Today.AddDays(-1));
        _clock.Setup(_ => _.FirstOfMonth).Returns(new DateTime(Today.Year, Today.Month, 1));

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);

        _controller = new LeaderboardController(
            _userRepository.Object,
            _internationalService.Object,
            _clock.Object,
            _gameCalendar.Object,
            _playerService.Object,
            _badgeService.Object,
            _leaderService.Object,
            _proposalService.Object,
            httpContextAccessor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    private void SetUser(ulong userId, UserTypes userType = UserTypes.StandardUser)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)userType).ToString())
        };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null));
    }

    private static ProposalResponse BuildResponse(ProposalDto dto, PlayerFullDto player)
    {
        var resources = new Mock<IStringLocalizer>();
        resources.Setup(r => r[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));
        return new ProposalResponse(dto, player, resources.Object, TestCountryContinents.Map);
    }

    // ------------------------------------------------------------- UserDay

    [Fact]
    public async Task UserDay_HappyPath_UsesTheSameCountryContinentsForDayboardAndProposals()
    {
        const ulong viewer = 1;
        const ulong viewedUser = 2;
        SetUser(viewer);

        var viewedUserDto = UserDtoBuilder.Valid().WithId(viewedUser).WithLogin("cible").WithType(UserTypes.StandardUser).Build();
        _userRepository.Setup(_ => _.GetUserByIdAsync(viewedUser)).ReturnsAsync(viewedUserDto);

        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(viewer, Today)).ReturnsAsync(DayGrantTypes.Admin);

        var playerFull = PlayerFullDtoBuilder.Valid().WithPlayer(PlayerDtoBuilder.Valid().WithCreator(99).Build()).Build();
        _playerService.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Today)).ReturnsAsync(playerFull);

        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);

        var dayboard = new Dayboard
        {
            Date = Today,
            Leaders =
            [
                new DayboardLeaderItem { UserId = viewedUser, UserName = "cible", Points = 750, Date = Today, Time = TimeSpan.FromMinutes(12) }
            ],
            Searchers = []
        };
        _leaderService.Setup(_ => _.GetDayboardAsync(Today, DayLeaderSorts.BestTime, TestCountryContinents.Map)).ReturnsAsync(dayboard);

        var proposals = new[]
        {
            BuildResponse(ProposalDtoBuilder.Valid().OfType(ProposalTypes.Continent).WithSuccessfulFlag(1).WithValue(((ulong)Continents.Europe).ToString()).Build(), playerFull)
        };
        _proposalService.Setup(_ => _.GetProposalsAsync(Today, viewedUser, TestCountryContinents.Map)).ReturnsAsync(proposals);

        var result = await _controller.UserDay(viewedUser, Today.ToString("yyyy-MM-dd"));

        var model = ((ViewResult)result).Model.Should().BeOfType<KikoleSite.ViewModels.UserDayModel>().Subject;
        model.UserLogin.Should().Be("cible");
        model.UserScore.Should().Be(750);
        model.ProposalDetails.Should().ContainSingle();
    }

    [Fact]
    public async Task UserDay_UnparsableDate_RedirectsToErrorIndex()
    {
        SetUser(1);

        var result = await _controller.UserDay(2, "not-a-date");

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "ErrorIndex" && r.ControllerName == "Home");
    }

    [Fact]
    public async Task UserDay_DateInTheFuture_RedirectsToErrorIndex()
    {
        SetUser(1);

        var result = await _controller.UserDay(2, Today.AddDays(1).ToString("yyyy-MM-dd"));

        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task UserDay_UnknownUser_RedirectsToErrorIndex()
    {
        SetUser(1);
        _userRepository.Setup(_ => _.GetUserByIdAsync(2)).ReturnsAsync((UserDto?)null);

        var result = await _controller.UserDay(2, Today.ToString("yyyy-MM-dd"));

        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task UserDay_TargetIsAdministrator_RedirectsToErrorIndex()
    {
        SetUser(1);
        var admin = UserDtoBuilder.Valid().WithId(2).WithType(UserTypes.Administrator).Build();
        _userRepository.Setup(_ => _.GetUserByIdAsync(2)).ReturnsAsync(admin);

        var result = await _controller.UserDay(2, Today.ToString("yyyy-MM-dd"));

        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task UserDay_GrantNotAllowed_RedirectsToErrorIndex()
    {
        SetUser(1);
        var target = UserDtoBuilder.Valid().WithId(2).WithType(UserTypes.StandardUser).Build();
        _userRepository.Setup(_ => _.GetUserByIdAsync(2)).ReturnsAsync(target);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(1, Today)).ReturnsAsync(DayGrantTypes.None);

        var result = await _controller.UserDay(2, Today.ToString("yyyy-MM-dd"));

        result.Should().BeOfType<RedirectToActionResult>();
        _playerService.Verify(_ => _.GetPlayerOfTheDayFullInfoAsync(It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task UserDay_ViewingOwnCreation_RedirectsToErrorIndex()
    {
        const ulong viewer = 1;
        const ulong viewedUser = 2;
        SetUser(viewer);
        var target = UserDtoBuilder.Valid().WithId(viewedUser).WithType(UserTypes.StandardUser).Build();
        _userRepository.Setup(_ => _.GetUserByIdAsync(viewedUser)).ReturnsAsync(target);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(viewer, Today)).ReturnsAsync(DayGrantTypes.Admin);

        var playerFull = PlayerFullDtoBuilder.Valid().WithPlayer(PlayerDtoBuilder.Valid().WithCreator(viewedUser).Build()).Build();
        _playerService.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Today)).ReturnsAsync(playerFull);

        var result = await _controller.UserDay(viewedUser, Today.ToString("yyyy-MM-dd"));

        result.Should().BeOfType<RedirectToActionResult>();
        _internationalService.Verify(_ => _.GetCountryContinentsAsync(), Times.Never);
    }

    // ------------------------------------------------------------- GetGlobalLeaderboardDetailsAsync / GetDailyLeaderboardDetailsAsync

    [Fact]
    public async Task GetGlobalLeaderboardDetailsAsync_ReturnsTheLeaderboardFromTheService()
    {
        SetUser(1);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(1, Today)).ReturnsAsync(DayGrantTypes.Found);

        var items = new List<LeaderboardItem> { new() { UserId = 1, UserName = "joueur", Points = 100 } };
        _leaderService
            .Setup(_ => _.GetLeaderboardAsync(TestCalendar.FirstDate, Today, LeaderSorts.TotalPoints))
            .ReturnsAsync(items);

        var result = await _controller.GetGlobalLeaderboardDetailsAsync(LeaderSorts.TotalPoints, TestCalendar.FirstDate, Today);

        result.Value.Should().BeSameAs(items);
    }

    [Fact]
    public async Task GetDailyLeaderboardDetailsAsync_HiddenWhenNoGrantOnToday()
    {
        SetUser(1);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(1, Today)).ReturnsAsync(DayGrantTypes.None);

        var result = await _controller.GetDailyLeaderboardDetailsAsync(DayLeaderSorts.BestTime, Today);

        var dayboard = result.Value.Should().BeOfType<Dayboard>().Subject;
        dayboard.Hidden.Should().BeTrue();
        _leaderService.Verify(_ => _.GetDayboardAsync(It.IsAny<DateTime>(), It.IsAny<DayLeaderSorts>(), It.IsAny<IReadOnlyDictionary<ulong, ulong>>()), Times.Never);
    }

    // ------------------------------------------------------------- Index (userId = 0)

    [Fact]
    public async Task Index_WithoutUserId_BuildsTheDefaultModelFromDayboardLeaderboardAndPodiums()
    {
        SetUser(1);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(1, Today)).ReturnsAsync(DayGrantTypes.Found);

        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);

        var dayboard = new Dayboard { Date = Today, Leaders = [], Searchers = [] };
        _leaderService.Setup(_ => _.GetDayboardAsync(Today, DayLeaderSorts.BestTime, TestCountryContinents.Map)).ReturnsAsync(dayboard);

        _leaderService
            .Setup(_ => _.GetLeaderboardAsync(new DateTime(Today.Year, Today.Month, 1), Today, LeaderSorts.TotalPoints))
            .ReturnsAsync(new List<LeaderboardItem>());

        _leaderService.Setup(_ => _.GetPodiumsAsync()).ReturnsAsync(new Podiums
        {
            MonthlyPodiums = new Dictionary<(int month, int year), (User first, User second, User third)>(),
            OverallPodium = []
        });

        var result = await _controller.Index(userId: 0);

        var model = ((ViewResult)result).Model.Should().BeOfType<KikoleSite.ViewModels.LeaderboardModel>().Subject;
        model.Dayboard.Should().BeSameAs(dayboard);
        model.CurrentUserId.Should().Be(1);
    }
}
