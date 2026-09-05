using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Handlers;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services;

/// <summary>
/// Podiums mensuels depuis FirstMonth, plus un cumul global des medailles. Les depots
/// renvoient les memes donnees quel que soit le mois, ce qui permet de tester
/// l'accumulation sur plusieurs iterations.
/// </summary>
public class LeaderServicePodiumsTests
{
    private static readonly DateTime FirstMonth = TestCalendar.FirstMonth;

    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly Mock<ILeaderRepository> _leaderRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IProposalRepository> _proposalRepository = new();
    private readonly Mock<IPlayerHandler> _playerHandler = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly LeaderService _service;

    public LeaderServicePodiumsTests()
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
            _gameCalendar.Object,
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

        List<UserDto> dtos = [.. users
            .Select(u => UserDtoBuilder.Valid().WithId(u.id).WithLogin(u.login).WithUserTypeId((ulong)UserTypes.StandardUser).Build())];

        _userRepository
            .Setup(_ => _.GetUsersByIdsAsync(It.IsAny<IReadOnlyCollection<ulong>>()))
            .ReturnsAsync((IReadOnlyCollection<ulong> ids) => dtos.Where(u => ids.Contains(u.Id)).ToList());
    }

    // ------------------------------------------------------------- podium mensuel

    [Fact]
    public async Task AMonthWithFewerThanThreeRankedUsersHasNoPodium()
    {
        SetupMonths(1);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90));

        var podiums = await _service.GetPodiumsAsync();

        podiums.MonthlyPodiums.Should().BeEmpty();
    }

    [Fact]
    public async Task ThePodiumIsOrderedByPointsFirst()
    {
        SetupMonths(1);
        SetupContenders((1, "bronze", 100, 10), (2, "or", 900, 300), (3, "argent", 500, 20));

        var podiums = await _service.GetPodiumsAsync();

        var podium = podiums.MonthlyPodiums.Single().Value;
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

        var podiums = await _service.GetPodiumsAsync();

        var podium = podiums.MonthlyPodiums.Single().Value;
        podium.first.Login.Should().Be("rapide");
        podium.second.Login.Should().Be("lent");
    }

    [Fact]
    public async Task ThePodiumIsKeyedByMonthAndYear()
    {
        SetupMonths(1);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var podiums = await _service.GetPodiumsAsync();

        podiums.MonthlyPodiums.Should().ContainKey((FirstMonth.Month, FirstMonth.Year));
    }

    // ------------------------------------------------------------- cumul global

    [Fact]
    public async Task TheGlobalTableAccumulatesMedalsAcrossMonths()
    {
        // trois mois, meme classement a chaque fois
        SetupMonths(3);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var podiums = await _service.GetPodiumsAsync();

        podiums.MonthlyPodiums.Should().HaveCount(3);

        var global = podiums.OverallPodium.ToList();
        global.Single(g => g.user.Login == "a").first.Should().Be(3);
        global.Single(g => g.user.Login == "b").second.Should().Be(3);
        global.Single(g => g.user.Login == "c").third.Should().Be(3);
    }

    [Fact]
    public async Task TheGlobalTableIsOrderedByGoldThenSilverThenBronze()
    {
        SetupMonths(2);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var podiums = await _service.GetPodiumsAsync();

        podiums.OverallPodium.Select(g => g.user.Login)
            .Should().ContainInOrder("a", "b", "c");
    }

    [Fact]
    public async Task EachUserAppearsOnlyOnceInTheGlobalTable()
    {
        SetupMonths(4);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var podiums = await _service.GetPodiumsAsync();

        podiums.OverallPodium.Should().HaveCount(3);
        podiums.OverallPodium.Select(g => g.user.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task AMonthWithoutAFullPodiumAwardsNoMedalAtAll()
    {
        // le cumul global est la somme des podiums mensuels : un mois ecarte de la
        // liste des podiums, faute de trois joueurs classes, ne doit rien crediter
        SetupMonths(3);
        SetupContenders((1, "a", 900, 60));

        var podiums = await _service.GetPodiumsAsync();

        podiums.MonthlyPodiums.Should().BeEmpty();
        podiums.OverallPodium.Should().BeEmpty();
    }

    [Fact]
    public async Task AMonthWithExactlyTwoContendersAwardsNoMedalEither()
    {
        // la borne est bien "trois places pourvues", pas "au moins une"
        SetupMonths(3);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90));

        var podiums = await _service.GetPodiumsAsync();

        podiums.MonthlyPodiums.Should().BeEmpty();
        podiums.OverallPodium.Should().BeEmpty();
    }

    [Fact]
    public async Task TheGlobalTableIsExactlyTheSumOfTheMonthlyPodiums()
    {
        SetupMonths(3);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        var podiums = await _service.GetPodiumsAsync();

        var golds = podiums.OverallPodium.Sum(g => g.first);
        var silvers = podiums.OverallPodium.Sum(g => g.second);
        var bronzes = podiums.OverallPodium.Sum(g => g.third);

        golds.Should().Be(podiums.MonthlyPodiums.Count);
        silvers.Should().Be(podiums.MonthlyPodiums.Count);
        bronzes.Should().Be(podiums.MonthlyPodiums.Count);
    }

    [Fact]
    public async Task TheCurrentMonthStopsAtYesterdayInsteadOfItsLastDay()
    {
        // le mois en cours n'est pas termine : la borne haute est la veille,
        // pour ne pas inclure une journee encore jouable
        SetupMonths(1);
        SetupContenders((1, "a", 900, 60), (2, "b", 500, 90), (3, "c", 100, 120));

        await _service.GetPodiumsAsync();

        _leaderRepository.Verify(
            _ => _.GetLeadersAsync(FirstMonth, FirstMonth.AddDays(10), true), Times.Once);
    }
}
