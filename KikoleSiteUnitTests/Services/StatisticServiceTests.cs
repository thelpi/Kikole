using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services;

/// <summary>
/// GetPlayersStatisticsAsync est la seule methode du service qui porte de la logique
/// (anonymisation du nom, tri, agregats par jour/au global) ; GetActiveUsersAsync et
/// GetPlayersDistributionAsync ne font que projeter le depot sans branche a caracteriser
/// ici (le tri par rang est deja couvert par CollectionHelperTests.SetPositions).
/// </summary>
public class StatisticServiceTests
{
    private static readonly DateTime Today = new(2026, 9, 10);

    private const ulong CreatorId = 10;
    private const string CreatorLogin = "createur";
    private const ulong ViewerId = 20;
    private const string AnonymizedName = "???";

    private readonly Mock<IStatisticRepository> _statisticRepository = new();
    private readonly Mock<IInternationalRepository> _internationalRepository = new();
    private readonly Mock<IClubRepository> _clubRepository = new();
    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILeaderRepository> _leaderRepository = new();
    private readonly Mock<IProposalRepository> _proposalRepository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly StatisticService _service;

    public StatisticServiceTests()
    {
        _clock.Setup(_ => _.Now).Returns(Today);
        _clock.Setup(_ => _.Yesterday).Returns(Today.AddDays(-1));

        _service = new StatisticService(
            _statisticRepository.Object,
            _internationalRepository.Object,
            _clubRepository.Object,
            _playerRepository.Object,
            _userRepository.Object,
            _leaderRepository.Object,
            _proposalRepository.Object,
            _clock.Object);

        _userRepository.Setup(_ => _.GetUserByIdAsync(CreatorId))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(CreatorId).WithLogin(CreatorLogin).Build());
    }

    private static PlayerDto Player(DateTime publicationDate, ulong id = 1, string name = "Zinédine Zidane")
        => PlayerDtoBuilder.Valid().WithId(id).WithName(name).WithCreator(CreatorId).WithPublicationDate(publicationDate).Build();

    private void SetupData(
        IReadOnlyCollection<PlayerDto> players,
        IReadOnlyCollection<ProposalDto>? proposals = null,
        IReadOnlyCollection<LeaderDto>? leaders = null)
    {
        _playerRepository.Setup(_ => _.GetPlayersOfTheDayAsync(null, Today.AddDays(-1)))
            .ReturnsAsync(players);
        _proposalRepository.Setup(_ => _.GetProposalsActivityAsync())
            .ReturnsAsync(proposals ?? new List<ProposalDto>());
        _leaderRepository.Setup(_ => _.GetLeadersAsync(null, null, false))
            .ReturnsAsync(leaders ?? new List<LeaderDto>());
    }

    private void SetupViewer(UserTypes type)
    {
        _userRepository.Setup(_ => _.GetUserByIdAsync(ViewerId))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(ViewerId).WithType(type).Build());
    }

    // ------------------------------------------------------------- anonymisation

    [Fact]
    public async Task AStandardViewerWhoNeverFoundIt_SeesTheAnonymizedName()
    {
        var day = Today.AddDays(-3);
        SetupData([Player(day)]);
        SetupViewer(UserTypes.StandardUser);

        var result = await _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, false);

        result.Should().ContainSingle().Which.Name.Should().Be(AnonymizedName);
    }

    [Fact]
    public async Task ALeaderOfThatDay_SeesTheRealName()
    {
        var day = Today.AddDays(-3);
        var leader = LeaderDtoBuilder.Valid().WithUserId(ViewerId).WithProposalDate(day).Build();
        SetupData([Player(day)], leaders: [leader]);
        SetupViewer(UserTypes.StandardUser);

        var result = await _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, false);

        result.Should().ContainSingle().Which.Name.Should().Be("Zinédine Zidane");
    }

    [Fact]
    public async Task TheCreatorHimself_SeesTheRealNameEvenWithoutHavingFoundIt()
    {
        var day = Today.AddDays(-3);
        SetupData([Player(day)]);
        // le createur regarde ses propres soumissions : re-setup un profil standard sous son id
        _userRepository.Setup(_ => _.GetUserByIdAsync(CreatorId))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(CreatorId).WithLogin(CreatorLogin).WithType(UserTypes.StandardUser).Build());

        var result = await _service.GetPlayersStatisticsAsync(CreatorId, AnonymizedName, PlayerSorts.PublicationDate, false);

        result.Should().ContainSingle().Which.Name.Should().Be("Zinédine Zidane");
    }

    [Fact]
    public async Task AnAdmin_AlwaysSeesTheRealNameEvenWithoutHavingFoundIt()
    {
        var day = Today.AddDays(-3);
        SetupData([Player(day)]);
        SetupViewer(UserTypes.Administrator);

        var result = await _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, false);

        result.Should().ContainSingle().Which.Name.Should().Be("Zinédine Zidane");
    }

    // ------------------------------------------------------------- comptages

    [Fact]
    public async Task TriesAndSuccessesAreCountedSeparatelyForTheSameDayAndOverall()
    {
        var day = Today.AddDays(-5);
        var proposals = new List<ProposalDto>
        {
            // essais du jour meme (creation le jour de la proposition)
            ProposalDtoBuilder.Valid().WithUser(1).WithProposalDate(day).WithCreationDate(day).Build(),
            ProposalDtoBuilder.Valid().WithUser(2).WithProposalDate(day).WithCreationDate(day).Build(),
            // rattrapage : cree 2 jours plus tard, ne compte plus comme "du jour"
            ProposalDtoBuilder.Valid().WithUser(3).WithProposalDate(day).WithCreationDate(day.AddDays(2)).Build(),
        };
        var leaders = new List<LeaderDto>
        {
            LeaderDtoBuilder.Valid().WithUserId(1).WithProposalDate(day).WithCreationDate(day).WithTime(30).Build(),
            LeaderDtoBuilder.Valid().WithUserId(3).WithProposalDate(day).WithCreationDate(day.AddDays(2)).WithTime(90).Build(),
        };
        SetupData([Player(day)], proposals, leaders);
        SetupViewer(UserTypes.Administrator);

        var result = (await _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, false)).Single();

        result.TriesCountSameDay.Should().Be(2);
        result.TriesCountTotal.Should().Be(3);
        result.SuccessesCountSameDay.Should().Be(1);
        result.SuccessesCountTotal.Should().Be(2);
        result.BestTime.Should().Be(30);
    }

    [Fact]
    public async Task WhenNobodyEverFoundIt_AveragesAndBestTimeDefaultToZeroInsteadOfDividingByZero()
    {
        var day = Today.AddDays(-2);
        SetupData([Player(day)]);
        SetupViewer(UserTypes.Administrator);

        var result = (await _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, false)).Single();

        result.BestTime.Should().Be(0);
        result.AveragePointsSameDay.Should().Be(0);
        result.AveragePointsTotal.Should().Be(0);
        result.SuccessesCountSameDay.Should().Be(0);
        result.SuccessesCountTotal.Should().Be(0);
    }

    [Fact]
    public async Task AveragePointsAreComputedSeparatelyForTheSameDayAndOverall_AndTruncatedNotRounded()
    {
        var day = Today.AddDays(-2);
        var leaders = new List<LeaderDto>
        {
            LeaderDtoBuilder.Valid().WithUserId(1).WithProposalDate(day).WithCreationDate(day).WithPoints(1000).Build(),
            LeaderDtoBuilder.Valid().WithUserId(2).WithProposalDate(day).WithCreationDate(day).WithPoints(600).Build(),
            LeaderDtoBuilder.Valid().WithUserId(3).WithProposalDate(day).WithCreationDate(day.AddDays(3)).WithPoints(100).Build(),
        };
        SetupData([Player(day)], leaders: leaders);
        SetupViewer(UserTypes.Administrator);

        var result = (await _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, false)).Single();

        result.AveragePointsSameDay.Should().Be(800); // (1000 + 600) / 2
        // (1000 + 600 + 100) / 3 = 566.67 ; la conversion (int) tronque, ne fait pas
        // d'arrondi (contrairement a UserStatsModel.GetRate, qui utilise Math.Round)
        result.AveragePointsTotal.Should().Be(566);
    }

    // ------------------------------------------------------------- tri

    [Fact]
    public async Task ResultsAreSortedByTheRequestedCriterion_AndReversedWhenDescending()
    {
        var earlier = Today.AddDays(-10);
        var later = Today.AddDays(-2);
        SetupData([
            Player(earlier, id: 1, name: "A"),
            Player(later, id: 2, name: "B")
        ]);
        SetupViewer(UserTypes.Administrator);

        var ascending = await _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, false);
        var descending = await _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, true);

        ascending.Select(r => r.Name).Should().ContainInOrder("A", "B");
        descending.Select(r => r.Name).Should().ContainInOrder("B", "A");
    }

    // ------------------------------------------------------------- invariantes

    [Fact]
    public async Task WhenTheCreatorIsMissing_ThrowsAndNamesTheMissingCreator()
    {
        const ulong missingCreatorId = 999;
        var day = Today.AddDays(-1);
        var player = PlayerDtoBuilder.Valid().WithId(1).WithCreator(missingCreatorId).WithPublicationDate(day).Build();
        SetupData([player]);
        SetupViewer(UserTypes.Administrator);
        _userRepository.Setup(_ => _.GetUserByIdAsync(missingCreatorId))
            .ReturnsAsync((UserDto?)null);

        Func<Task> act = () => _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, false);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{missingCreatorId}*");
    }

    [Fact]
    public async Task DaysBeforeIsComputedFromTheClockAndThePublicationDate()
    {
        var day = Today.AddDays(-7);
        SetupData([Player(day)]);
        SetupViewer(UserTypes.Administrator);

        var result = (await _service.GetPlayersStatisticsAsync(ViewerId, AnonymizedName, PlayerSorts.PublicationDate, false)).Single();

        result.DaysBefore.Should().Be(7);
    }
}
