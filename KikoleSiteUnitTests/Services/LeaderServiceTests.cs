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

public class LeaderServiceTests
{
    private static readonly DateTime Day = TestCalendar.FirstDate;

    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly Mock<ILeaderRepository> _leaderRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IProposalRepository> _proposalRepository = new();
    private readonly Mock<IPlayerHandler> _playerHandler = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly LeaderService _service;

    public LeaderServiceTests()
    {
        _clock.Setup(_ => _.Today).Returns(Day);
        _clock.Setup(_ => _.Yesterday).Returns(Day.AddDays(-1));
        _clock.Setup(_ => _.FirstOfMonth).Returns(new DateTime(Day.Year, Day.Month, 1));

        var localizer = new Mock<IStringLocalizer<Translations>>();
        localizer.Setup(_ => _[It.IsAny<string>()])
            .Returns<string>(k => new LocalizedString(k, k));

        _proposalRepository
            .Setup(_ => _.GetDaysCountWithProposalAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<bool>()))
            .ReturnsAsync(0);

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

    private void SetupUsers(params (ulong id, string login)[] users)
    {
        List<UserDto> dtos = [.. users
            .Select(u => UserDtoBuilder.Valid().WithId(u.id).WithLogin(u.login).WithUserTypeId((ulong)UserTypes.StandardUser).Build())];

        // le depot filtre par id demande : le mock doit faire pareil, sinon un test qui
        // enregistre 3 utilisateurs mais n'en reclame que 2 en verrait quand meme 3.
        _userRepository
            .Setup(_ => _.GetUsersByIdsAsync(It.IsAny<IReadOnlyCollection<ulong>>()))
            .ReturnsAsync((IReadOnlyCollection<ulong> ids) => dtos.Where(u => ids.Contains(u.Id)).ToList());
    }

    private static LeaderDto Leader(ulong userId, ushort points, int minutes, DateTime? date = null)
    {
        return LeaderDtoBuilder.Valid().WithUserId(userId).WithPoints(points).WithTime(minutes).WithProposalDate(date ?? Day).WithCreationDate((date ?? Day).AddMinutes(minutes)).Build();
    }

    // ------------------------------------------------------------- GetLeaderboardAsync

    private void SetupLeaderboard(IEnumerable<LeaderDto> leaders, IEnumerable<PlayerDto> players)
    {
        _leaderRepository
            .Setup(_ => _.GetLeadersAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()))
            .ReturnsAsync(leaders.ToList());
        _playerRepository
            .Setup(_ => _.GetPlayersOfTheDayAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(players.ToList());
    }

    [Fact]
    public async Task GetLeaderboardAsync_WhenDatesAreInverted_TheyAreSwapped()
    {
        SetupLeaderboard(new List<LeaderDto>(), new List<PlayerDto>());

        await _service
            .GetLeaderboardAsync(Day.AddDays(10), Day, LeaderSorts.TotalPoints);

        _leaderRepository.Verify(
            _ => _.GetLeadersAsync(Day, Day.AddDays(10), It.IsAny<bool>()), Times.Once);
    }

    [Theory]
    [InlineData(LeaderSorts.SuccessCountOverall, false)]
    [InlineData(LeaderSorts.TotalPointsOverall, false)]
    [InlineData(LeaderSorts.SuccessCount, true)]
    [InlineData(LeaderSorts.TotalPoints, true)]
    [InlineData(LeaderSorts.BestTime, true)]
    public async Task GetLeaderboardAsync_OnlyTheOverallSortsIncludeCatchUpAnswers(
        LeaderSorts sort, bool expectedOnTimeOnly)
    {
        SetupLeaderboard(new List<LeaderDto>(), new List<PlayerDto>());

        await _service.GetLeaderboardAsync(Day, Day, sort);

        _leaderRepository.Verify(
            _ => _.GetLeadersAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), expectedOnTimeOnly),
            Times.Once);
    }

    [Fact]
    public async Task GetLeaderboardAsync_SumsFoundPointsAndSubmissionPoints()
    {
        SetupUsers((1, "joueur"));
        SetupLeaderboard(
            new[] { Leader(1, 800, 60), Leader(1, 500, 30, Day.AddDays(1)) },
            new[] { PlayerDtoBuilder.Valid().WithId(9).WithCreator(1).WithPublicationDate(Day.AddDays(2)).Build() });

        var result = await _service
            .GetLeaderboardAsync(Day, Day.AddDays(2), LeaderSorts.TotalPoints);

        var item = result.Single();
        item.Points.Should().Be(800 + 500 + ScoreCalculator.SubmissionPoints);
        item.KikolesFound.Should().Be(2);
        item.KikolesProposed.Should().Be(1);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ACreatorWhoNeverPlayedStillAppears()
    {
        // le classement melange les joueurs et les createurs : un createur qui n'a
        // jamais joue sur la periode doit quand meme apparaitre avec ses points
        SetupUsers((5, "createur"));
        SetupLeaderboard(
            new List<LeaderDto>(),
            new[] { PlayerDtoBuilder.Valid().WithId(9).WithCreator(5).WithPublicationDate(Day).Build() });

        var result = await _service
            .GetLeaderboardAsync(Day, Day, LeaderSorts.TotalPoints);

        result.Single().Points.Should().Be(ScoreCalculator.SubmissionPoints);
        result.Single().KikolesFound.Should().Be(0);
    }

    [Fact]
    public async Task GetLeaderboardAsync_SomeoneWhoNeverFoundAnythingGetsTheWorstPossibleTime()
    {
        SetupUsers((5, "createur"));
        SetupLeaderboard(
            new List<LeaderDto>(),
            new[] { PlayerDtoBuilder.Valid().WithId(9).WithCreator(5).WithPublicationDate(Day).Build() });

        var result = await _service
            .GetLeaderboardAsync(Day, Day, LeaderSorts.BestTime);

        result.Single().BestTime.Should().Be(new TimeSpan(23, 59, 59));
    }

    [Fact]
    public async Task GetLeaderboardAsync_KeepsTheFastestTimeOfThePeriod()
    {
        SetupUsers((1, "joueur"));
        SetupLeaderboard(
            new[] { Leader(1, 800, 300), Leader(1, 500, 120, Day.AddDays(1)) },
            new List<PlayerDto>());

        var result = await _service
            .GetLeaderboardAsync(Day, Day.AddDays(1), LeaderSorts.BestTime);

        result.Single().BestTime.Should().Be(new TimeSpan(2, 0, 0));
    }

    [Fact]
    public async Task GetLeaderboardAsync_RanksByPointsDescending()
    {
        SetupUsers((1, "petit"), (2, "gros"));
        SetupLeaderboard(new[] { Leader(1, 300, 60), Leader(2, 900, 90) }, new List<PlayerDto>());

        var result = await _service
            .GetLeaderboardAsync(Day, Day, LeaderSorts.TotalPoints);

        result.Single(_ => _.UserName == "gros").Rank.Should().Be(1);
        result.Single(_ => _.UserName == "petit").Rank.Should().Be(2);
    }

    [Fact]
    public async Task GetLeaderboardAsync_RanksByTimeAscending()
    {
        SetupUsers((1, "rapide"), (2, "lent"));
        SetupLeaderboard(new[] { Leader(1, 300, 30), Leader(2, 900, 600) }, new List<PlayerDto>());

        var result = await _service
            .GetLeaderboardAsync(Day, Day, LeaderSorts.BestTime);

        result.Single(_ => _.UserName == "rapide").Rank.Should().Be(1);
        result.Single(_ => _.UserName == "lent").Rank.Should().Be(2);
    }

    [Fact]
    public async Task GetLeaderboardAsync_AdministratorsAreExcluded()
    {
        _userRepository
            .Setup(_ => _.GetUsersByIdsAsync(It.IsAny<IReadOnlyCollection<ulong>>()))
            .ReturnsAsync(new List<UserDto>
            {
                UserDtoBuilder.Valid().WithId(1).WithLogin("admin").WithUserTypeId((ulong)UserTypes.Administrator).Build()
            });
        SetupLeaderboard(new[] { Leader(1, 900, 60) }, new List<PlayerDto>());

        var result = await _service
            .GetLeaderboardAsync(Day, Day, LeaderSorts.TotalPoints);

        result.Should().BeEmpty();
    }

    // ------------------------------------------------------------- ComputeMissingLeadersAsync

    private static ProposalDto Proposal(ProposalTypes type, bool successful, int minutes)
    {
        return ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)type).WithSuccessfulFlag((byte)(successful ? 1 : 0)).WithProposalDate(Day).WithCreationDate(Day.AddMinutes(minutes)).Build();
    }

    private void SetupMissingLeader(params ProposalDto[] proposals)
    {
        var player = PlayerDtoBuilder.Valid().WithId(1).WithName("Zidane").WithAllowedNames("zidane").WithPublicationDate(Day).WithYearOfBirth(1972).WithCountryId((ulong)Countries.FRA).WithContinentId((ulong)Continents.Europe).WithPositionId((ulong)Positions.Midfielder).Build();

        _playerRepository
            .Setup(_ => _.GetPlayersOfTheDayAsync(null, Day))
            .ReturnsAsync(new List<PlayerDto> { player });
        _playerHandler
            .Setup(_ => _.GetPlayerFullInfoAsync(It.IsAny<PlayerDto>()))
            .ReturnsAsync(new PlayerFullDto
            {
                Player = player,
                Clubs = [],
                PlayerClubs = []
            });
        _proposalRepository
            .Setup(_ => _.GetMissingUsersAsLeaderAsync(Day))
            .ReturnsAsync(new List<ulong> { 7 });
        _proposalRepository
            .Setup(_ => _.GetProposalsAsync(Day, 7UL))
            .ReturnsAsync(proposals.ToList());
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_RebuildsTheScoreFromTheProposals()
    {
        SetupMissingLeader(
            Proposal(ProposalTypes.Country, false, 5),   // -25
            Proposal(ProposalTypes.Club, false, 10),     // -50
            Proposal(ProposalTypes.Name, true, 90));

        await _service.ComputeMissingLeadersAsync();

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l =>
                l.UserId == 7 && l.Points == 925 && l.Time == 90)),
            Times.Once);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_StopsAtTheWinningProposal()
    {
        // commentaire du code : "we had for a while a bug of proposals after the
        // player has been found" — les propositions posterieures sont ignorees
        SetupMissingLeader(
            Proposal(ProposalTypes.Name, true, 30),
            Proposal(ProposalTypes.Club, false, 60));

        await _service.ComputeMissingLeadersAsync();

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l => l.Points == 1000)), Times.Once);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_ClampsTheScoreAtZero()
    {
        SetupMissingLeader(
            Proposal(ProposalTypes.Name, false, 5),
            Proposal(ProposalTypes.Name, false, 6),
            Proposal(ProposalTypes.Name, false, 7),
            Proposal(ProposalTypes.Name, true, 8));

        await _service.ComputeMissingLeadersAsync();

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l => l.Points == 0)), Times.Once);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_WhenTheAnswerWasNeverFound_NoLeaderIsCreated()
    {
        SetupMissingLeader(Proposal(ProposalTypes.Club, false, 5));

        await _service.ComputeMissingLeadersAsync();

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.IsAny<LeaderDto>()), Times.Never);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_RoundsTheElapsedMinutesUp()
    {
        SetupMissingLeader(ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)ProposalTypes.Name).WithSuccessfulFlag(1).WithProposalDate(Day).WithCreationDate(Day.AddMinutes(61).AddSeconds(30)).Build());

        await _service.ComputeMissingLeadersAsync();

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l => l.Time == 62)), Times.Once);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_ChargesForCluesLikeTheLiveScoring()
    {
        // l'indice et le classement sont enregistres avec Successful = 1 : seul le
        // partage du calcul avec le score affiche garantit qu'ils restent factures
        SetupMissingLeader(
            Proposal(ProposalTypes.Clue, true, 5),        // -50 %
            Proposal(ProposalTypes.Name, true, 90));

        await _service.ComputeMissingLeadersAsync();

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l => l.Points == 500)), Times.Once);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_ChargesForTheLeaderboardPurchaseToo()
    {
        SetupMissingLeader(
            Proposal(ProposalTypes.Leaderboard, true, 5),  // -25
            Proposal(ProposalTypes.Name, true, 90));

        await _service.ComputeMissingLeadersAsync();

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l => l.Points == 975)), Times.Once);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_MatchesTheLiveScoringExactly()
    {
        // garde-fou anti-regression : les deux chemins doivent produire le meme score
        // sur une meme sequence de propositions
        var proposals = new[]
        {
            Proposal(ProposalTypes.Country, false, 1),   // -25
            Proposal(ProposalTypes.Clue, true, 2),       // -50 %
            Proposal(ProposalTypes.Club, false, 3),      // -50
            Proposal(ProposalTypes.Name, true, 90)
        };

        SetupMissingLeader(proposals);

        var playerInfo = new PlayerFullDto
        {
            Player = PlayerDtoBuilder.Valid().WithId(1).WithName("Zidane").WithAllowedNames("zidane").WithYearOfBirth(1972).WithCountryId((ulong)Countries.FRA).WithContinentId((ulong)Continents.Europe).WithPositionId((ulong)Positions.Midfielder).Build(),
            Clubs = [],
            PlayerClubs = []
        };

        var localizer = new Mock<IStringLocalizer<Translations>>();
        localizer.Setup(_ => _[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

        ScoreCalculator.GetProposalResponsesWithPoints(
            proposals, playerInfo, out var livePoints, localizer.Object);

        await _service.ComputeMissingLeadersAsync();

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l => l.Points == livePoints)), Times.Once);
    }

    // ------------------------------------------------------------- GetDayboardAsync

    private void SetupDayboard(
        IEnumerable<LeaderDto> leaders,
        IEnumerable<ProposalDto> proposals,
        ulong creatorId)
    {
        _leaderRepository.Setup(_ => _.GetLeadersAtDateAsync(Day, false)).ReturnsAsync(leaders.ToList());
        _proposalRepository.Setup(_ => _.GetProposalsAsync(Day, false)).ReturnsAsync(proposals.ToList());
        _playerHandler.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Day))
            .ReturnsAsync(new PlayerFullDto
            {
                Player = PlayerDtoBuilder.Valid().WithId(1).WithName("Zidane").WithAllowedNames("zidane").WithCreator(creatorId).Build(),
                Clubs = [],
                PlayerClubs = []
            });
    }

    [Fact]
    public async Task GetDayboardAsync_TheCreatorIsAddedToTheBoardWithSubmissionPoints()
    {
        SetupUsers((1, "trouveur"), (5, "createur"));
        SetupDayboard(new[] { Leader(1, 800, 60) }, new List<ProposalDto>(), creatorId: 5);

        var result = await _service.GetDayboardAsync(Day, DayLeaderSorts.TotalPoints);

        var creator = result.Leaders.Single(_ => _.IsCreator);
        creator.UserName.Should().Be("createur");
        creator.Points.Should().Be(ScoreCalculator.SubmissionPoints);
        creator.Time.Should().Be(new TimeSpan(23, 59, 59));
    }

    [Fact]
    public async Task GetDayboardAsync_RanksTheCreatorAlongsideTheFinders()
    {
        SetupUsers((1, "trouveur"), (5, "createur"));
        SetupDayboard(new[] { Leader(1, 800, 60) }, new List<ProposalDto>(), creatorId: 5);

        var result = await _service.GetDayboardAsync(Day, DayLeaderSorts.TotalPoints);

        result.Leaders.Single(_ => _.IsCreator).Rank.Should().Be(1);
        result.Leaders.Single(_ => !_.IsCreator).Rank.Should().Be(2);
    }

    [Fact]
    public async Task GetDayboardAsync_SomeoneWhoOnlySearchedIsListedApart()
    {
        SetupUsers((1, "trouveur"), (2, "chercheur"), (5, "createur"));
        SetupDayboard(
            new[] { Leader(1, 800, 60) },
            new[]
            {
                ProposalDtoBuilder.Valid().WithUser(2).WithProposalTypeId((ulong)ProposalTypes.Club).WithValue("Barcelone").WithSuccessfulFlag(0).WithProposalDate(Day).WithCreationDate(Day.AddMinutes(20)).Build()
            },
            creatorId: 5);

        var result = await _service.GetDayboardAsync(Day, DayLeaderSorts.TotalPoints);

        result.Searchers.Should().ContainSingle();
        result.Searchers.Single().UserName.Should().Be("chercheur");
        result.Searchers.Single().Points.Should().Be(950);
        result.Leaders.Should().NotContain(_ => _.UserName == "chercheur");
    }

    [Fact]
    public async Task GetDayboardAsync_AFinderIsNotAlsoListedAsASearcher()
    {
        SetupUsers((1, "trouveur"), (5, "createur"));
        SetupDayboard(
            new[] { Leader(1, 800, 60) },
            new[]
            {
                ProposalDtoBuilder.Valid().WithUser(1).WithProposalTypeId((ulong)ProposalTypes.Name).WithValue("Zidane").WithSuccessfulFlag(1).WithProposalDate(Day).WithCreationDate(Day.AddMinutes(60)).Build()
            },
            creatorId: 5);

        var result = await _service.GetDayboardAsync(Day, DayLeaderSorts.TotalPoints);

        result.Searchers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDayboardAsync_KeepsTheRequestedDayAndSort()
    {
        SetupUsers((5, "createur"));
        SetupDayboard(new List<LeaderDto>(), new List<ProposalDto>(), creatorId: 5);

        var result = await _service
            .GetDayboardAsync(Day.AddHours(15), DayLeaderSorts.BestTime);

        result.Date.Should().Be(Day);
        result.Sort.Should().Be(DayLeaderSorts.BestTime);
    }
}
