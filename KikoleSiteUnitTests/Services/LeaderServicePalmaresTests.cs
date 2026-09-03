using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Handlers;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services;

/// <summary>
/// Palmares mensuel : un podium par mois depuis FirstMonth, plus un cumul global
/// des medailles. Les depots renvoient les memes donnees quel que soit le mois,
/// ce qui permet de tester l'accumulation sur plusieurs iterations.
/// </summary>
public class LeaderServicePalmaresTests
{
    private static readonly DateTime FirstMonth = ProposalChart.FirstMonth;

    private readonly Mock<IPlayerRepository> _playerRepository = new Mock<IPlayerRepository>();
    private readonly Mock<ILeaderRepository> _leaderRepository = new Mock<ILeaderRepository>();
    private readonly Mock<IUserRepository> _userRepository = new Mock<IUserRepository>();
    private readonly Mock<IProposalRepository> _proposalRepository = new Mock<IProposalRepository>();
    private readonly Mock<IPlayerHandler> _playerHandler = new Mock<IPlayerHandler>();
    private readonly Mock<IClock> _clock = new Mock<IClock>();
    private readonly LeaderService _service;

    public LeaderServicePalmaresTests()
    {
        var localizer = new Mock<IStringLocalizer<Translations>>();
        localizer.Setup(_ => _[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

        _proposalRepository
            .Setup(_ => _.GetDaysCountWithProposalAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<bool>()))
            .ReturnsAsync(0);
        _playerRepository
            .Setup(_ => _.GetPlayersOfTheDayAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<PlayerDto>());

        _service = new LeaderService(
            _playerRepository.Object,
            _leaderRepository.Object,
            _userRepository.Object,
            _proposalRepository.Object,
            _clock.Object,
            localizer.Object,
            _playerHandler.Object);
    }

    /// <param name="monthsSpan">Nombre de mois couverts (1 = uniquement FirstMonth).</param>
    private void SetupMonths(int monthsSpan)
    {
        var currentMonth = FirstMonth.AddMonths(monthsSpan - 1);
        _clock.Setup(_ => _.FirstOfMonth).Returns(currentMonth);
        _clock.Setup(_ => _.Yesterday).Returns(currentMonth.AddDays(10));
    }

    private void SetupContenders(params (ulong id, string login, ushort points, int minutes)[] users)
    {
        _leaderRepository
            .Setup(_ => _.GetLeadersAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()))
            .ReturnsAsync(users
                .Select(u => LeaderDtoBuilder.Valid().WithUserId(u.id).WithPoints(u.points).WithTime(u.minutes).WithProposalDate(FirstMonth).WithCreationDate(FirstMonth.AddMinutes(u.minutes)).Build())
                .ToList());

        foreach (var u in users)
        {
            _userRepository.Setup(_ => _.GetUserByIdAsync(u.id))
                .ReturnsAsync(UserDtoBuilder.Valid().WithId(u.id).WithLogin(u.login).WithUserTypeId((ulong)UserTypes.StandardUser).Build());
        }
    }

    // ------------------------------------------------------------- podium mensuel

    [Fact]
    public async Task AMonthWithFewerThanThreeRankedUsersHasNoPodium()
    {
        SetupMonths(1);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90));

        var palmares = await _service.GetPalmaresAsync();

        palmares.MonthlyPalmares.Should().BeEmpty();
    }

    [Fact]
    public async Task ThePodiumIsOrderedByPointsFirst()
    {
        SetupMonths(1);
        SetupContenders((1, "bronze", 100, 10), (2, "or", 900, 300), (3, "argent", 500, 20));

        var palmares = await _service.GetPalmaresAsync();

        var podium = palmares.MonthlyPalmares.Single().Value;
        podium.first.Login.Should().Be("or");
        podium.second.Login.Should().Be("argent");
        podium.third.Login.Should().Be("bronze");
    }

    [Fact]
    public async Task AtEqualPointsTheFastestWins()
    {
        // les criteres s'enchainent : points, puis nombre de trouvailles, puis temps
        SetupMonths(1);
        SetupContenders((1, "lent", 500, 300), (2, "rapide", 500, 30), (3, "dernier", 100, 10));

        var palmares = await _service.GetPalmaresAsync();

        var podium = palmares.MonthlyPalmares.Single().Value;
        podium.first.Login.Should().Be("rapide");
        podium.second.Login.Should().Be("lent");
    }

    [Fact]
    public async Task ThePodiumIsKeyedByMonthAndYear()
    {
        SetupMonths(1);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var palmares = await _service.GetPalmaresAsync();

        palmares.MonthlyPalmares.Should().ContainKey((FirstMonth.Month, FirstMonth.Year));
    }

    // ------------------------------------------------------------- cumul global

    [Fact]
    public async Task TheGlobalTableAccumulatesMedalsAcrossMonths()
    {
        // trois mois, meme classement a chaque fois
        SetupMonths(3);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var palmares = await _service.GetPalmaresAsync();

        palmares.MonthlyPalmares.Should().HaveCount(3);

        var global = palmares.GlobalPalmares.ToList();
        global.Single(g => g.user.Login == "a").first.Should().Be(3);
        global.Single(g => g.user.Login == "b").second.Should().Be(3);
        global.Single(g => g.user.Login == "c").third.Should().Be(3);
    }

    [Fact]
    public async Task TheGlobalTableIsOrderedByGoldThenSilverThenBronze()
    {
        SetupMonths(2);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var palmares = await _service.GetPalmaresAsync();

        palmares.GlobalPalmares.Select(g => g.user.Login)
            .Should().ContainInOrder("a", "b", "c");
    }

    [Fact]
    public async Task EachUserAppearsOnlyOnceInTheGlobalTable()
    {
        SetupMonths(4);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var palmares = await _service.GetPalmaresAsync();

        palmares.GlobalPalmares.Should().HaveCount(3);
        palmares.GlobalPalmares.Select(g => g.user.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task AMonthWithoutAFullPodiumAwardsNoMedalAtAll()
    {
        // le cumul global est la somme des podiums mensuels : un mois ecarte de la
        // liste des podiums, faute de trois joueurs classes, ne doit rien crediter
        SetupMonths(3);
        SetupContenders((1, "a", 900, 60));

        var palmares = await _service.GetPalmaresAsync();

        palmares.MonthlyPalmares.Should().BeEmpty();
        palmares.GlobalPalmares.Should().BeEmpty();
    }

    [Fact]
    public async Task AMonthWithExactlyTwoContendersAwardsNoMedalEither()
    {
        // la borne est bien "trois places pourvues", pas "au moins une"
        SetupMonths(3);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90));

        var palmares = await _service.GetPalmaresAsync();

        palmares.MonthlyPalmares.Should().BeEmpty();
        palmares.GlobalPalmares.Should().BeEmpty();
    }

    [Fact]
    public async Task TheGlobalTableIsExactlyTheSumOfTheMonthlyPodiums()
    {
        SetupMonths(3);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var palmares = await _service.GetPalmaresAsync();

        var golds = palmares.GlobalPalmares.Sum(g => g.first);
        var silvers = palmares.GlobalPalmares.Sum(g => g.second);
        var bronzes = palmares.GlobalPalmares.Sum(g => g.third);

        golds.Should().Be(palmares.MonthlyPalmares.Count);
        silvers.Should().Be(palmares.MonthlyPalmares.Count);
        bronzes.Should().Be(palmares.MonthlyPalmares.Count);
    }

    [Fact]
    public async Task TheCurrentMonthStopsAtYesterdayInsteadOfItsLastDay()
    {
        // le mois en cours n'est pas termine : la borne haute est la veille,
        // pour ne pas inclure une journee encore jouable
        SetupMonths(1);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        await _service.GetPalmaresAsync();

        _leaderRepository.Verify(
            _ => _.GetLeadersAsync(FirstMonth, FirstMonth.AddDays(10), true), Times.Once);
    }
}
