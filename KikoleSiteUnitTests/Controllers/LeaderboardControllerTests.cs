using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KikoleSite;
using KikoleSite.Controllers;
using KikoleSite.Helpers;
using KikoleSite.Identity;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
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
    private static readonly DateOnly Today = TestCalendar.FirstDate.AddDays(30);

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
        _clock.Setup(_ => _.FirstOfMonth).Returns(new DateOnly(Today.Year, Today.Month, 1));

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

    private void SetUpUserDayNeighbours(ulong viewer, ulong viewedUser, params (int dayOffset, bool played, DayGrantTypes grant)[] days)
    {
        SetUser(viewer);

        _userRepository
            .Setup(_ => _.GetUserByIdAsync(viewedUser))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(viewedUser).WithLogin("cible").WithType(UserTypes.StandardUser).Build());

        var playerFull = PlayerFullDtoBuilder.Valid().WithPlayer(PlayerDtoBuilder.Valid().WithCreator(99).Build()).Build();
        _playerService.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(It.IsAny<DateOnly>())).ReturnsAsync(playerFull);
        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);
        _leaderService
            .Setup(_ => _.GetDayboardAsync(It.IsAny<DateOnly>(), DayLeaderSorts.BestTime, TestCountryContinents.Map))
            .ReturnsAsync(new Dayboard { Date = Today, Leaders = [], Searchers = [] });
        _proposalService
            .Setup(_ => _.GetProposalsAsync(It.IsAny<DateOnly>(), viewedUser, TestCountryContinents.Map))
            .ReturnsAsync(Array.Empty<ProposalResponse>());

        // le jour consulte (Today) et le jour "courant" sont toujours consultables
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(viewer, Today)).ReturnsAsync(DayGrantTypes.Admin);
        foreach (var (offset, _, grant) in days)
            _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(viewer, Today.AddDays(offset))).ReturnsAsync(grant);

        var stats = new UserStat(
            days.Select(d => new DailyUserStat(viewedUser, Today.AddDays(d.dayOffset), "***", false, d.played, [], null)).ToList(),
            "cible",
            Today.AddYears(-1).ToDateTime(TimeOnly.MinValue));
        _leaderService
            .Setup(_ => _.GetUserStatisticsAsync(viewedUser, viewer, "***", true))
            .ReturnsAsync(stats);
    }

    [Fact]
    public async Task UserDay_NeighbourDays_SkipDaysNotPlayedAndDaysTheViewerCannotOpen()
    {
        // avant : -1 non joue, -2 joue mais non consultable, -3 joue et consultable
        SetUpUserDayNeighbours(1, 2,
            (-3, true, DayGrantTypes.Found),
            (-2, true, DayGrantTypes.None),
            (-1, false, DayGrantTypes.Found));

        var result = await _controller.UserDay(2, Today.ToString("yyyy-MM-dd"));

        var model = ((ViewResult)result).Model.Should().BeOfType<KikoleSite.ViewModels.UserDayModel>().Subject;
        model.UserId.Should().Be(2);
        model.PreviousDate.Should().Be(Today.AddDays(-3));
        model.NextDate.Should().BeNull();
    }

    [Fact]
    public async Task UserDay_NeighbourDays_LinkToTheNextPlayedDayWhenThereIsOne()
    {
        // on consulte Today-2 : le jour suivant joue et consultable est Today-1, pas Today
        SetUpUserDayNeighbours(1, 2,
            (-2, true, DayGrantTypes.Found),
            (-1, true, DayGrantTypes.Found),
            (0, true, DayGrantTypes.Found));

        var result = await _controller.UserDay(2, Today.AddDays(-2).ToString("yyyy-MM-dd"));

        var model = ((ViewResult)result).Model.Should().BeOfType<KikoleSite.ViewModels.UserDayModel>().Subject;
        model.PreviousDate.Should().BeNull();
        model.NextDate.Should().Be(Today.AddDays(-1));
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
        _playerService.Verify(_ => _.GetPlayerOfTheDayFullInfoAsync(It.IsAny<DateOnly>()), Times.Never);
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

        var row = result.Value.Should().BeAssignableTo<IReadOnlyCollection<KikoleSite.ViewModels.LeaderboardRow>>()
            .Subject.Should().ContainSingle().Subject;
        row.UserId.Should().Be(1);
        row.UserName.Should().Be("joueur");
        row.Points.Should().Be(100);
    }

    [Fact]
    public async Task GetGlobalLeaderboardDetailsAsync_BadgePercentage_SkipsTheTodayGrantCheck()
    {
        // aucune garde "today" ni bornage de dates pour ce tri : aucun appel au grant
        SetUser(1);
        _leaderService
            .Setup(_ => _.GetLeaderboardAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), LeaderSorts.BadgePercentage))
            .ReturnsAsync(new List<LeaderboardItem> { new() { UserId = 1, UserName = "joueur", BadgesFound = 2 } });

        var result = await _controller.GetGlobalLeaderboardDetailsAsync(LeaderSorts.BadgePercentage, Today, Today);

        result.Value.Should().BeAssignableTo<IReadOnlyCollection<KikoleSite.ViewModels.LeaderboardRow>>()
            .Subject.Should().ContainSingle().Which.BadgesFound.Should().Be(2);
        _proposalService.Verify(_ => _.GetGrantAccessForDayAsync(It.IsAny<ulong>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task GetDailyLeaderboardDetailsAsync_HiddenWhenNoGrantOnToday()
    {
        SetUser(1);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(1, Today)).ReturnsAsync(DayGrantTypes.None);

        var result = await _controller.GetDailyLeaderboardDetailsAsync(DayLeaderSorts.BestTime, Today);

        var dayboard = result.Value.Should().BeOfType<KikoleSite.ViewModels.DayboardModel>().Subject;
        dayboard.Hidden.Should().BeTrue();
        _leaderService.Verify(_ => _.GetDayboardAsync(It.IsAny<DateOnly>(), It.IsAny<DayLeaderSorts>(), It.IsAny<IReadOnlyDictionary<ulong, ulong>>()), Times.Never);
    }

    // ------------------------------------------------------------- UnlockDailyLeaderboardAsync

    [Fact]
    public async Task UnlockDailyLeaderboardAsync_HappyPath_SubmitsALeaderboardProposalForTodayAndReturnsSuccess()
    {
        const ulong userId = 1;
        SetUser(userId);

        var playerFull = PlayerFullDtoBuilder.Valid().Build();
        _playerService.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Today)).ReturnsAsync(playerFull);
        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);

        var response = BuildResponse(
            ProposalDtoBuilder.Valid().OfType(ProposalTypes.Leaderboard).WithValue("GetLeaderboard").WithSuccessfulFlag(1).Build(),
            playerFull);
        _proposalService
            .Setup(_ => _.ManageProposalResponseAsync(It.IsAny<ProposalRequest>(), userId, playerFull, TestCountryContinents.Map))
            .ReturnsAsync((response, Array.Empty<ProposalDto>(), (LeaderDto?)null));

        var result = await _controller.UnlockDailyLeaderboardAsync();

        result.Value.Should().BeEquivalentTo(new { success = true });
        // toujours le jour courant (DaysBeforeNow = 0), jamais le jour consulte par ailleurs
        // sur la page - "acheter" le classement ne s'applique qu'au jour du jeu en cours
        _proposalService.Verify(_ => _.ManageProposalResponseAsync(
            It.Is<ProposalRequest>(r => r.DaysBeforeNow == 0
                && r.ProposalType == ProposalTypes.Leaderboard
                && r.Value == "GetLeaderboard"),
            userId, playerFull, TestCountryContinents.Map), Times.Once);
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
            .Setup(_ => _.GetLeaderboardAsync(new DateOnly(Today.Year, Today.Month, 1), Today, LeaderSorts.TotalPoints))
            .ReturnsAsync(new List<LeaderboardItem>());

        _leaderService.Setup(_ => _.GetPodiumsAsync()).ReturnsAsync(new Podiums
        {
            MonthlyPodiums = new Dictionary<(int month, int year), (User first, User second, User third)>(),
            OverallPodium = []
        });

        var result = await _controller.Index(userId: 0);

        var model = ((ViewResult)result).Model.Should().BeOfType<KikoleSite.ViewModels.LeaderboardModel>().Subject;
        model.Dayboard.Date.Should().Be(dayboard.Date);
        model.CurrentUserId.Should().Be(1);
    }

    // ------------------------------------------------------------- Index (userId != 0)

    [Fact]
    public async Task Index_WithUnknownUserId_FallsBackToTheDefaultModel()
    {
        SetUser(1);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(1, Today)).ReturnsAsync(DayGrantTypes.Found);
        _leaderService
            .Setup(_ => _.GetUserStatisticsAsync(99, 1, "***", true))
            .ReturnsAsync((UserStat?)null);

        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);
        var dayboard = new Dayboard { Date = Today, Leaders = [], Searchers = [] };
        _leaderService.Setup(_ => _.GetDayboardAsync(Today, DayLeaderSorts.BestTime, TestCountryContinents.Map)).ReturnsAsync(dayboard);
        _leaderService
            .Setup(_ => _.GetLeaderboardAsync(new DateOnly(Today.Year, Today.Month, 1), Today, LeaderSorts.TotalPoints))
            .ReturnsAsync(new List<LeaderboardItem>());
        _leaderService.Setup(_ => _.GetPodiumsAsync()).ReturnsAsync(new Podiums
        {
            MonthlyPodiums = new Dictionary<(int month, int year), (User first, User second, User third)>(),
            OverallPodium = []
        });

        var result = await _controller.Index(userId: 99);

        var model = ((ViewResult)result).Model.Should().BeOfType<KikoleSite.ViewModels.LeaderboardModel>().Subject;
        model.Dayboard.Date.Should().Be(dayboard.Date);
        _badgeService.Verify(_ => _.GetUserBadgesAsync(It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<Languages>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Index_WithKnownUserId_BuildsTheUserStatsModel()
    {
        const ulong viewer = 1;
        const ulong viewedUser = 2;
        SetUser(viewer);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(viewer, Today)).ReturnsAsync(DayGrantTypes.Found);

        var stats = new UserStat(
            [new DailyUserStat(Today, "Zidane", 500)],
            "cible",
            Today.AddYears(-1).ToDateTime(TimeOnly.MinValue));
        _leaderService
            .Setup(_ => _.GetUserStatisticsAsync(viewedUser, viewer, "***", true))
            .ReturnsAsync(stats);

        var language = ViewHelper.GetLanguage();
        _badgeService
            .Setup(_ => _.GetUserBadgesAsync(viewedUser, viewer, language, true))
            .ReturnsAsync(Array.Empty<UserBadge>());
        _badgeService.Setup(_ => _.GetAllBadgesAsync(language)).ReturnsAsync(Array.Empty<Badge>());

        var result = await _controller.Index(userId: viewedUser);

        var model = ((ViewResult)result).Model.Should().BeOfType<KikoleSite.ViewModels.UserStatsModel>().Subject;
        model.Login.Should().Be("cible");
        model.IsHimself.Should().BeFalse();
        model.TotalPoints.Should().Be(500);
    }

    [Theory]
    [InlineData(DayGrantTypes.None, false)]
    [InlineData(DayGrantTypes.PaidBoard, false)] // classement achete sans avoir trouve : pas la reponse
    [InlineData(DayGrantTypes.Found, true)]
    [InlineData(DayGrantTypes.Creator, true)]
    [InlineData(DayGrantTypes.Admin, true)]
    public async Task Index_WithKnownUserId_TodaysBadgesOfOthersAreOnlyRevealedToThoseWhoKnowTheAnswer(
        DayGrantTypes grant, bool expectedToSeeTodaysBadges)
    {
        const ulong viewer = 1;
        const ulong viewedUser = 2;
        SetUser(viewer);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(viewer, Today)).ReturnsAsync(grant);

        var stats = new UserStat(
            [new DailyUserStat(Today, "Zidane", 500)],
            "cible",
            Today.AddYears(-1).ToDateTime(TimeOnly.MinValue));
        _leaderService
            .Setup(_ => _.GetUserStatisticsAsync(viewedUser, viewer, "***", It.IsAny<bool>()))
            .ReturnsAsync(stats);

        var language = ViewHelper.GetLanguage();
        _badgeService
            .Setup(_ => _.GetUserBadgesAsync(viewedUser, viewer, language, It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<UserBadge>());
        _badgeService.Setup(_ => _.GetAllBadgesAsync(language)).ReturnsAsync(Array.Empty<Badge>());

        await _controller.Index(userId: viewedUser);

        _badgeService.Verify(
            _ => _.GetUserBadgesAsync(viewedUser, viewer, language, expectedToSeeTodaysBadges), Times.Once);
    }

    [Fact]
    public async Task Index_WithKnownUserId_ListsTheDailyStatsMostRecentFirst()
    {
        const ulong viewer = 1;
        const ulong viewedUser = 2;
        SetUser(viewer);
        _proposalService.Setup(_ => _.GetGrantAccessForDayAsync(viewer, Today)).ReturnsAsync(DayGrantTypes.Found);

        var stats = new UserStat(
            [
                new DailyUserStat(Today.AddDays(-2), "Ancien", 100),
                new DailyUserStat(Today, "Recent", 300),
                new DailyUserStat(Today.AddDays(-1), "Milieu", 200)
            ],
            "cible",
            Today.AddYears(-1).ToDateTime(TimeOnly.MinValue));
        _leaderService
            .Setup(_ => _.GetUserStatisticsAsync(viewedUser, viewer, "***", true))
            .ReturnsAsync(stats);

        var language = ViewHelper.GetLanguage();
        _badgeService
            .Setup(_ => _.GetUserBadgesAsync(viewedUser, viewer, language, true))
            .ReturnsAsync(Array.Empty<UserBadge>());
        _badgeService.Setup(_ => _.GetAllBadgesAsync(language)).ReturnsAsync(Array.Empty<Badge>());

        var result = await _controller.Index(userId: viewedUser);

        var model = ((ViewResult)result).Model.Should().BeOfType<KikoleSite.ViewModels.UserStatsModel>().Subject;
        model.Stats.Select(s => s.Answer).Should().ContainInOrder("Recent", "Milieu", "Ancien");
    }
}
