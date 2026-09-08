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
using KikoleSite.Models.Enums;
using KikoleSite.Models.Statistics;
using KikoleSite.Repositories;
using KikoleSite.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Controllers;

/// <summary>
/// Couvre <see cref="StatisticsController"/>, jusqu'ici hors perimetre (aucun test de
/// controleur n'existait avant la levee du blocage Identity, cf. TODO). Les 4 actions ne
/// font que mettre en forme le retour du service en JSON - les tests verifient ce
/// mappage, pas la logique statistique elle-meme (deja testee dans StatisticServiceTests).
/// </summary>
public class StatisticsControllerTests
{
    private readonly DefaultHttpContext _httpContext = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IInternationalService> _internationalService = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly Mock<IPlayerService> _playerService = new();
    private readonly Mock<IBadgeService> _badgeService = new();
    private readonly Mock<IStatisticService> _statisticService = new();
    private readonly StatisticsController _controller;

    public StatisticsControllerTests()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);

        _controller = new StatisticsController(
            _userRepository.Object,
            _internationalService.Object,
            _clock.Object,
            _gameCalendar.Object,
            _playerService.Object,
            _badgeService.Object,
            _statisticService.Object,
            httpContextAccessor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)UserTypes.Administrator).ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null));
    }

    [Fact]
    public void Stats_ReturnsTheView()
    {
        var result = _controller.Stats();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task GetStatisticPlayersDistribution_ShapesEachDistributionAsKeyValuePairs()
    {
        var country = new Country(CountryDtoBuilder.Valid().WithName("France").Build());
        var club = new Club(ClubDtoBuilder.Valid().WithName("Real Madrid").Build(), []);

        _statisticService
            .Setup(_ => _.GetPlayersDistributionAsync(1, It.IsAny<Languages>(), 25))
            .ReturnsAsync(new PlayersDistribution
            {
                TotalPlayersCount = 20,
                CountriesDistribution = [new PlayersDistributionItem<Country>(country, 10, 20)],
                ClubsDistribution = [new PlayersDistributionItem<Club>(club, 4, 20)],
                DecadesDistribution = [new PlayersDistributionItem<int>(1990, 5, 20)],
                PositionsDistribution = [new PlayersDistributionItem<Positions>(Positions.Midfielder, 8, 20)]
            });

        var result = await _controller.GetStatisticPlayersDistribution();

        dynamic value = result.Value!;
        ((IEnumerable<KeyValuePair<string, decimal>>)value.country).Should().ContainSingle(kv => kv.Key == "France" && kv.Value == 50m);
        ((IEnumerable<KeyValuePair<string, decimal>>)value.club).Should().ContainSingle(kv => kv.Key == "Real Madrid" && kv.Value == 4m);
        ((IEnumerable<KeyValuePair<string, decimal>>)value.decade).Should().ContainSingle(kv => kv.Key == "1990" && kv.Value == 25m);
        ((IEnumerable<KeyValuePair<string, decimal>>)value.position).Should().ContainSingle(kv => kv.Key == Positions.Midfielder.GetLabel() && kv.Value == 40m);
    }

    [Fact]
    public async Task GetStatisticActiveUsers_ShapesMonthlyWeeklyDailyBuckets()
    {
        var day = new DateTime(2026, 9, 1);
        _statisticService
            .Setup(_ => _.GetActiveUsersAsync(null, It.IsAny<DateTime>()))
            .ReturnsAsync(new ActiveUsers
            {
                MonthlyDatas = new Dictionary<(int y, int m), int> { { (2026, 9), 12 } },
                WeeklyDatas = new Dictionary<(int y, int w), int> { { (2026, 36), 7 } },
                DailyDatas = new Dictionary<DateTime, int> { { day, 3 } }
            });

        var result = await _controller.GetStatisticActiveUsers();

        dynamic value = result.Value!;
        ((IEnumerable<KeyValuePair<string, int>>)value.monthly).Should().ContainSingle(kv => kv.Key == "09 (26)" && kv.Value == 12);
        ((IEnumerable<KeyValuePair<string, int>>)value.weekly).Should().ContainSingle(kv => kv.Key == "36 (26)" && kv.Value == 7);
        ((IEnumerable<KeyValuePair<string, int>>)value.daily).Should().ContainSingle(kv => kv.Value == 3);
    }

    [Fact]
    public async Task GetKikolesStatisticsAsync_PassesSortAndDirectionThroughToTheService()
    {
        var stats = new List<PlayerStatistics>
        {
            new() { Date = DateTime.Today, Name = "Zidane", Creator = "createur" }
        };
        _statisticService
            .Setup(_ => _.GetPlayersStatisticsAsync(1, "***", PlayerSorts.BestTime, true))
            .ReturnsAsync(stats);

        var result = await _controller.GetKikolesStatisticsAsync(PlayerSorts.BestTime, desc: true);

        result.Value.Should().BeSameAs(stats);
    }
}
