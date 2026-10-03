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
    private static readonly DateOnly Day = TestCalendar.FirstDate;

    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly Mock<ILeaderRepository> _leaderRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IProposalRepository> _proposalRepository = new();
    private readonly Mock<IPlayerHandler> _playerHandler = new();
    private readonly Mock<IBadgeRepository> _badgeRepository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly LeaderService _service;

    public LeaderServiceTests()
    {
        _clock.Setup(_ => _.Today).Returns(Day);
        _clock.Setup(_ => _.Yesterday).Returns(Day.AddDays(-1));
        _clock.Setup(_ => _.FirstOfMonth).Returns(new DateOnly(Day.Year, Day.Month, 1));

        var localizer = new Mock<IStringLocalizer<Translations>>();
        localizer.Setup(_ => _[It.IsAny<string>()])
            .Returns<string>(k => new LocalizedString(k, k));

        _proposalRepository
            .Setup(_ => _.GetDaysCountWithProposalAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<ulong>(), It.IsAny<bool>()))
            .ReturnsAsync(0);

        _service = new LeaderService(
            _playerRepository.Object,
            _leaderRepository.Object,
            _userRepository.Object,
            _proposalRepository.Object,
            _clock.Object,
            _gameCalendar.Object,
            localizer.Object,
            _playerHandler.Object,
            _badgeRepository.Object);
    }

    private void SetupUsers(params (ulong id, string login)[] users)
    {
        List<UserDto> dtos = [.. users
            .Select(u => UserDtoBuilder.Valid().WithId(u.id).WithLogin(u.login).WithUserTypeId((ulong)UserTypes.StandardUser).Build())];

        // le depot filtre par id demande : le mock doit faire pareil, sinon un test qui
        // enregistre 3 utilisateurs mais n'en reclame que 2 en verrait quand meme 3.
        _userRepository
            .Setup(_ => _.GetUsersByIdsIncludingDisabledAsync(It.IsAny<IReadOnlyCollection<ulong>>()))
            .ReturnsAsync((IReadOnlyCollection<ulong> ids) => dtos.Where(u => ids.Contains(u.Id)).ToList());
    }

    private void SetupDisabledCreator(ulong creatorId)
    {
        List<UserDto> dtos =
        [
            UserDtoBuilder.Valid().WithId(1).WithLogin("trouveur").Build(),
            UserDtoBuilder.Valid().WithId(creatorId).WithLogin("createur").WithDisabled().Build()
        ];

        _userRepository
            .Setup(_ => _.GetUsersByIdsIncludingDisabledAsync(It.IsAny<IReadOnlyCollection<ulong>>()))
            .ReturnsAsync((IReadOnlyCollection<ulong> ids) => dtos.Where(u => ids.Contains(u.Id)).ToList());
    }

    private static LeaderDto Leader(ulong userId, ushort points, int minutes, DateOnly? date = null)
    {
        return LeaderDtoBuilder.Valid().WithUserId(userId).WithPoints(points).WithTime(minutes).WithProposalDate(date ?? Day).WithCreationDate((date ?? Day).ToDateTime(TimeOnly.MinValue).AddMinutes(minutes)).Build();
    }

    // ------------------------------------------------------------- GetLeaderboardAsync

    private void SetupLeaderboard(IEnumerable<LeaderDto> leaders, IEnumerable<PlayerDto> players)
    {
        _leaderRepository
            .Setup(_ => _.GetLeadersAsync(It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<bool>()))
            .ReturnsAsync(leaders.ToList());
        _playerRepository
            .Setup(_ => _.GetPlayersOfTheDayAsync(It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
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
            _ => _.GetLeadersAsync(It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), expectedOnTimeOnly),
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
    public async Task GetLeaderboardAsync_ADisabledCreatorIsExcluded()
    {
        SetupDisabledCreator(creatorId: 5);
        SetupLeaderboard(
            new[] { Leader(1, 800, 60) },
            new[] { PlayerDtoBuilder.Valid().WithId(9).WithCreator(5).WithPublicationDate(Day).Build() });

        var result = await _service
            .GetLeaderboardAsync(Day, Day, LeaderSorts.TotalPoints);

        result.Single().UserName.Should().Be("trouveur");
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
            .Setup(_ => _.GetUsersByIdsIncludingDisabledAsync(It.IsAny<IReadOnlyCollection<ulong>>()))
            .ReturnsAsync(new List<UserDto>
            {
                UserDtoBuilder.Valid().WithId(1).WithLogin("admin").WithUserTypeId((ulong)UserTypes.Administrator).Build()
            });
        SetupLeaderboard(new[] { Leader(1, 900, 60) }, new List<PlayerDto>());

        var result = await _service
            .GetLeaderboardAsync(Day, Day, LeaderSorts.TotalPoints);

        result.Should().BeEmpty();
    }

    // ------------------------------------------------------------- GetUserStatisticsAsync

    [Fact]
    public async Task GetUserStatisticsAsync_AdministratorHasNoPublicProfile_LikeAnUnknownUser()
    {
        _userRepository
            .Setup(_ => _.GetUserByIdAsync(1))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(1).WithLogin("admin").WithUserTypeId((ulong)UserTypes.Administrator).Build());
        _userRepository
            .Setup(_ => _.GetUserByIdAsync(404))
            .ReturnsAsync((UserDto?)null);

        var admin = await _service.GetUserStatisticsAsync(1, 0, "***", false);
        var unknown = await _service.GetUserStatisticsAsync(404, 0, "***", false);

        admin.Should().BeNull();
        unknown.Should().BeNull();
    }

    // ------------------------------------------------------------- ComputeMissingLeadersAsync

    private static ProposalDto Proposal(ProposalTypes type, bool successful, int minutes)
    {
        return ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)type).WithSuccessfulFlag((byte)(successful ? 1 : 0)).WithProposalDate(Day).WithCreationDate(Day.ToDateTime(TimeOnly.MinValue).AddMinutes(minutes)).Build();
    }

    private void SetupMissingLeader(params ProposalDto[] proposals)
    {
        var player = PlayerDtoBuilder.Valid().WithId(1).WithName("Zidane").WithAllowedNames("zidane").WithPublicationDate(Day).WithYearOfBirth(1972).WithCountryId((ulong)Countries.FRA).WithPositionId((ulong)Positions.Midfielder).Build();

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

        await _service.ComputeMissingLeadersAsync(TestCountryContinents.Map);

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

        await _service.ComputeMissingLeadersAsync(TestCountryContinents.Map);

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

        await _service.ComputeMissingLeadersAsync(TestCountryContinents.Map);

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l => l.Points == 0)), Times.Once);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_WhenTheAnswerWasNeverFound_NoLeaderIsCreated()
    {
        SetupMissingLeader(Proposal(ProposalTypes.Club, false, 5));

        await _service.ComputeMissingLeadersAsync(TestCountryContinents.Map);

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.IsAny<LeaderDto>()), Times.Never);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_RoundsTheElapsedMinutesUp()
    {
        SetupMissingLeader(ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)ProposalTypes.Name).WithSuccessfulFlag(1).WithProposalDate(Day).WithCreationDate(Day.ToDateTime(TimeOnly.MinValue).AddMinutes(61).AddSeconds(30)).Build());

        await _service.ComputeMissingLeadersAsync(TestCountryContinents.Map);

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

        await _service.ComputeMissingLeadersAsync(TestCountryContinents.Map);

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l => l.Points == 500)), Times.Once);
    }

    [Fact]
    public async Task ComputeMissingLeadersAsync_ChargesForTheLeaderboardPurchaseToo()
    {
        SetupMissingLeader(
            Proposal(ProposalTypes.Leaderboard, true, 5),  // -25
            Proposal(ProposalTypes.Name, true, 90));

        await _service.ComputeMissingLeadersAsync(TestCountryContinents.Map);

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
            Player = PlayerDtoBuilder.Valid().WithId(1).WithName("Zidane").WithAllowedNames("zidane").WithYearOfBirth(1972).WithCountryId((ulong)Countries.FRA).WithPositionId((ulong)Positions.Midfielder).Build(),
            Clubs = [],
            PlayerClubs = []
        };

        var localizer = new Mock<IStringLocalizer<Translations>>();
        localizer.Setup(_ => _[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

        ScoreCalculator.GetProposalResponsesWithPoints(
            proposals, playerInfo, out var livePoints, localizer.Object, TestCountryContinents.Map);

        await _service.ComputeMissingLeadersAsync(TestCountryContinents.Map);

        _leaderRepository.Verify(
            _ => _.CreateLeaderAsync(It.Is<LeaderDto>(l => l.Points == livePoints)), Times.Once);
    }

    // ------------------------------------------------------------- GetUserStreakAsync

    private void SetupStreak(DateOnly today, params DateOnly[] foundDates)
    {
        _clock.Setup(_ => _.Today).Returns(today);
        _leaderRepository
            .Setup(_ => _.GetUserLeadersAsync(TestCalendar.FirstDate, today, true, 1))
            .ReturnsAsync(foundDates.Select(d => Leader(1, 800, 60, d)).ToList());
    }

    [Fact]
    public async Task GetUserStreakAsync_NeverFoundAnything_HasNoStreak()
    {
        SetupStreak(Day.AddDays(10));

        var result = await _service.GetUserStreakAsync(1);

        result.HasStreak.Should().BeFalse();
        result.Current.Should().Be(0);
        result.Best.Should().Be(0);
    }

    [Fact]
    public async Task GetUserStreakAsync_ConsecutiveDaysEndingToday_AreAllCounted()
    {
        var today = Day.AddDays(10);
        SetupStreak(today, today, today.AddDays(-1), today.AddDays(-2));

        var result = await _service.GetUserStreakAsync(1);

        result.Current.Should().Be(3);
        result.Best.Should().Be(3);
    }

    [Fact]
    public async Task GetUserStreakAsync_TodayNotYetPlayed_DoesNotBreakTheStreak()
    {
        // le jour courant n'est pas encore joue : la serie arretee hier reste valable
        var today = Day.AddDays(10);
        SetupStreak(today, today.AddDays(-1), today.AddDays(-2));

        var result = await _service.GetUserStreakAsync(1);

        result.Current.Should().Be(2);
    }

    [Fact]
    public async Task GetUserStreakAsync_AGapInThePast_BreaksTheStreak()
    {
        var today = Day.AddDays(10);
        SetupStreak(today, today, today.AddDays(-5));

        var result = await _service.GetUserStreakAsync(1);

        result.Current.Should().Be(1);
    }

    [Fact]
    public async Task GetUserStreakAsync_BestStreakKeepsTheLongestHistoricalRun()
    {
        var today = Day.AddDays(10);
        SetupStreak(today,
            today, // serie en cours : 1 jour
            today.AddDays(-5), today.AddDays(-6), today.AddDays(-7), today.AddDays(-8)); // ancienne serie : 4 jours

        var result = await _service.GetUserStreakAsync(1);

        result.Current.Should().Be(1);
        result.Best.Should().Be(4);
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

        var result = await _service.GetDayboardAsync(Day, DayLeaderSorts.TotalPoints, TestCountryContinents.Map);

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

        var result = await _service.GetDayboardAsync(Day, DayLeaderSorts.TotalPoints, TestCountryContinents.Map);

        result.Leaders.Single(_ => _.IsCreator).Rank.Should().Be(1);
        result.Leaders.Single(_ => !_.IsCreator).Rank.Should().Be(2);
    }

    [Fact]
    public async Task GetDayboardAsync_ADisabledCreatorIsLeftOffTheBoard()
    {
        SetupDisabledCreator(creatorId: 5);
        SetupDayboard(new[] { Leader(1, 800, 60) }, new List<ProposalDto>(), creatorId: 5);

        var result = await _service.GetDayboardAsync(Day, DayLeaderSorts.TotalPoints, TestCountryContinents.Map);

        result.Leaders.Should().ContainSingle().Which.UserName.Should().Be("trouveur");
    }

    [Fact]
    public async Task GetDayboardAsync_SomeoneWhoOnlySearchedIsListedApart()
    {
        SetupUsers((1, "trouveur"), (2, "chercheur"), (5, "createur"));
        SetupDayboard(
            new[] { Leader(1, 800, 60) },
            new[]
            {
                ProposalDtoBuilder.Valid().WithUser(2).WithProposalTypeId((ulong)ProposalTypes.Club).WithValue("Barcelone").WithSuccessfulFlag(0).WithProposalDate(Day).WithCreationDate(Day.ToDateTime(TimeOnly.MinValue).AddMinutes(20)).Build()
            },
            creatorId: 5);

        var result = await _service.GetDayboardAsync(Day, DayLeaderSorts.TotalPoints, TestCountryContinents.Map);

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
                ProposalDtoBuilder.Valid().WithUser(1).WithProposalTypeId((ulong)ProposalTypes.Name).WithValue("Zidane").WithSuccessfulFlag(1).WithProposalDate(Day).WithCreationDate(Day.ToDateTime(TimeOnly.MinValue).AddMinutes(60)).Build()
            },
            creatorId: 5);

        var result = await _service.GetDayboardAsync(Day, DayLeaderSorts.TotalPoints, TestCountryContinents.Map);

        result.Searchers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDayboardAsync_KeepsTheRequestedDayAndSort()
    {
        SetupUsers((5, "createur"));
        SetupDayboard(new List<LeaderDto>(), new List<ProposalDto>(), creatorId: 5);

        var result = await _service
            .GetDayboardAsync(Day, DayLeaderSorts.BestTime, TestCountryContinents.Map);

        result.Date.Should().Be(Day);
        result.Sort.Should().Be(DayLeaderSorts.BestTime);
    }

    // ------------------------------------------------------------- GetLeaderboardAsync (BadgePercentage)

    private void SetupBadgePercentagePopulation(params (ulong id, string login)[] users)
    {
        SetupUsers(users);
        _leaderRepository
            .Setup(_ => _.GetLeadersAsync(null, null, false))
            .ReturnsAsync(users.Select(u => LeaderDtoBuilder.Valid().WithUserId(u.id).Build()).ToList());
        _playerRepository
            .Setup(_ => _.GetPlayersOfTheDayAsync(null, null))
            .ReturnsAsync(new List<PlayerDto>());
    }

    private void SetupActiveBadges(params ulong[] badgeIds)
    {
        _badgeRepository
            .Setup(_ => _.GetBadgesAsync(true))
            .ReturnsAsync(badgeIds.Select(id => BadgeDtoBuilder.Valid().WithId(id).Build()).ToList());
    }

    private void SetupUserBadges(params (ulong userId, ulong badgeId, DateOnly date)[] rows)
    {
        _badgeRepository
            .Setup(_ => _.GetAllUserBadgesAsync())
            .ReturnsAsync(rows.Select(r => new UserBadgeDto { UserId = r.userId, BadgeId = r.badgeId, GetDate = r.date }).ToList());
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_ComputesFoundMissingAndPercentage()
    {
        SetupBadgePercentagePopulation((1, "joueur"));
        SetupActiveBadges(1, 2, 3, 4);
        SetupUserBadges((1, 1, Day), (1, 2, Day));

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        var item = result.Single();
        item.BadgesFound.Should().Be(2);
        item.BadgesMissing.Should().Be(2);
        item.BadgePercentage.Should().Be(50);
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_IgnoresTheDateRangeArguments()
    {
        // cumule sur toute la partie : les dates passees par l'appelant (meme tres
        // eloignees) n'ont aucun effet sur ce tri
        SetupBadgePercentagePopulation((1, "joueur"));
        SetupActiveBadges(1, 2);
        SetupUserBadges((1, 1, Day));

        var result = await _service.GetLeaderboardAsync(Day.AddYears(5), Day.AddYears(5), LeaderSorts.BadgePercentage);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_HiddenBadgesCountInTheTotalForEveryone()
    {
        SetupBadgePercentagePopulation((1, "joueur"));
        _badgeRepository
            .Setup(_ => _.GetBadgesAsync(true))
            .ReturnsAsync(new List<BadgeDto>
            {
                BadgeDtoBuilder.Valid().WithId(1).Build(),
                BadgeDtoBuilder.Valid().WithId(2).Hidden().Build()
            });
        SetupUserBadges((1, 1, Day));

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        var item = result.Single();
        item.BadgesMissing.Should().Be(1); // le badge cache compte dans le total de tout le monde
        item.BadgePercentage.Should().Be(50);
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_DisabledBadgesAreExcludedFromTheTotal()
    {
        SetupBadgePercentagePopulation((1, "joueur"));
        _badgeRepository
            .Setup(_ => _.GetBadgesAsync(true))
            .ReturnsAsync(new List<BadgeDto> { BadgeDtoBuilder.Valid().WithId(1).Build() });
            // badge 2 desactive : absent de GetBadgesAsync(true), comme en reel
        SetupUserBadges((1, 1, Day), (1, 2, Day)); // vieille ligne pour le badge 2, desormais desactive

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        var item = result.Single();
        item.BadgesFound.Should().Be(1);
        item.BadgesMissing.Should().Be(0);
        item.BadgePercentage.Should().Be(100);
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_UserWithNoBadgesHasNoAverageRarity()
    {
        SetupBadgePercentagePopulation((1, "joueur"));
        SetupActiveBadges(1, 2);
        SetupUserBadges();

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        result.Single().AverageBadgeRarity.Should().BeNull();
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_RarerBadgeGivesAHigherAverageRarity()
    {
        // badge 1 : tout le monde l'a (rarete 0 %) ; badge 2 : a est le seul a l'avoir (100 %)
        SetupBadgePercentagePopulation((1, "a"), (2, "b"));
        SetupActiveBadges(1, 2);
        SetupUserBadges((1, 1, Day), (2, 1, Day), (1, 2, Day));

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        result.Single(_ => _.UserId == 1).AverageBadgeRarity.Should().Be(50); // moyenne de (0 %, 100 %)
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_ASoleHolderGetsFullRarity()
    {
        // le detenteur lui-meme n'est pas compte parmi "les autres" : un badge unique vaut
        // 100 %, pas 1 - 1/N
        SetupBadgePercentagePopulation((1, "a"), (2, "b"), (3, "c"));
        SetupActiveBadges(1, 2);
        SetupUserBadges((1, 1, Day), (1, 2, Day));

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        result.Single(_ => _.UserId == 1).AverageBadgeRarity.Should().Be(100);
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_TheOnlyPlayerOfThePopulationIsUnique()
    {
        // population d'un seul joueur : pas d'"autres" a comparer (evite 0/0), unique par defaut
        SetupBadgePercentagePopulation((1, "seul"));
        SetupActiveBadges(1);
        SetupUserBadges((1, 1, Day));

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        result.Single().AverageBadgeRarity.Should().Be(100);
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_RanksByPercentageThenByAverageRarity()
    {
        // a : badges 1+2, b : badges 1+3, c : badge 2 seul. Detenteurs : badge 1 = a,b ;
        // badge 2 = a,c ; badge 3 = b seul. Rarete moyenne : a = 50 %, b = 75 %, c = 50 %.
        SetupBadgePercentagePopulation((1, "a"), (2, "b"), (3, "c"));
        SetupActiveBadges(1, 2, 3);
        SetupUserBadges(
            (1, 1, Day), (1, 2, Day),
            (2, 1, Day), (2, 3, Day),
            (3, 2, Day));

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        result.Single(_ => _.UserId == 2).Rank.Should().Be(1); // autant de badges que a, mais plus rares
        result.Single(_ => _.UserId == 1).Rank.Should().Be(2);
        result.Single(_ => _.UserId == 3).Rank.Should().Be(3); // moins de badges : jamais devant, meme rare
        result.Select(_ => _.UserId).Should().ContainInOrder(2UL, 1UL, 3UL);
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_SameBadgeCountAndSameRarityShareTheSameRank()
    {
        SetupBadgePercentagePopulation((1, "a"), (2, "b"));
        SetupActiveBadges(1, 2);
        SetupUserBadges(
            (1, 1, Day), (1, 2, Day),
            (2, 1, Day), (2, 2, Day));

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        result.Should().OnlyContain(_ => _.Rank == 1);
    }

    [Fact]
    public async Task GetLeaderboardAsync_BadgePercentage_ObtentionDateDoesNotInfluenceTheRank()
    {
        // meme badges, dates d'obtention tres differentes : la date n'est ni affichee ni un
        // critere de depart, donc egalite parfaite
        SetupBadgePercentagePopulation((1, "a"), (2, "b"));
        SetupActiveBadges(1);
        SetupUserBadges((1, 1, Day), (2, 1, Day.AddDays(200)));

        var result = await _service.GetLeaderboardAsync(Day, Day, LeaderSorts.BadgePercentage);

        result.Should().OnlyContain(_ => _.Rank == 1);
    }
}
