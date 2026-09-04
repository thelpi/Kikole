using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Handlers;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services;

public class BadgeServiceTests
{
    private static readonly DateTime Day = TestCalendar.FirstDate;
    private const ulong UserId = 7;

    private readonly Mock<IPlayerHandler> _playerHandler = new();
    private readonly Mock<IBadgeRepository> _badgeRepository = new();
    private readonly Mock<ILeaderRepository> _leaderRepository = new();
    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly Mock<IProposalRepository> _proposalRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly BadgeService _service;

    private readonly List<UserBadgeDto> _inserted = [];

    public BadgeServiceTests()
    {
        _clock.Setup(_ => _.Today).Returns(Day);
        _clock.Setup(_ => _.Now).Returns(Day);

        // tous les badges existent et sont anterieurs a la journee testee
        _badgeRepository.Setup(_ => _.GetBadgesAsync(It.IsAny<bool>()))
            .ReturnsAsync(Enum.GetValues(typeof(Badges)).Cast<Badges>()
                .Select(b => BadgeDtoBuilder.Valid().WithId((ulong)b).WithName(b.ToString()).WithDescription(b.ToString()).WithCreationDate(Day.AddYears(-1)).Build()).ToList());

        _badgeRepository.Setup(_ => _.CheckUserHasBadgeAsync(It.IsAny<ulong>(), It.IsAny<ulong>()))
            .ReturnsAsync(false);
        _badgeRepository.Setup(_ => _.GetUsersWithBadgeAsync(It.IsAny<ulong>()))
            .ReturnsAsync(new List<UserBadgeDto>());
        _badgeRepository.Setup(_ => _.GetUsersOfTheDayWithBadgeAsync(It.IsAny<ulong>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<UserBadgeDto>());
        _badgeRepository.Setup(_ => _.InsertUserBadgeAsync(It.IsAny<UserBadgeDto>()))
            .Callback<UserBadgeDto>(d => _inserted.Add(d))
            .Returns(Task.CompletedTask);

        _leaderRepository.Setup(_ => _.GetLeadersAtDateAsync(It.IsAny<DateTime>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<LeaderDto>());
        _playerRepository.Setup(_ => _.GetPlayersOfTheDayAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<PlayerDto>());

        _service = new BadgeService(
            _playerHandler.Object,
            _badgeRepository.Object,
            _leaderRepository.Object,
            _playerRepository.Object,
            _proposalRepository.Object,
            _userRepository.Object,
            _clock.Object,
            _gameCalendar.Object);
    }

    private static PlayerDto Player(ushort year = 1990, ulong? badgeId = null)
    {
        return PlayerDtoBuilder.Valid().WithId(1).WithName("Zidane").WithAllowedNames("zidane").WithYearOfBirth(year).WithPublicationDate(Day).WithCountryId((ulong)Countries.FRA).WithPositionId((ulong)Positions.Midfielder).WithBadge(badgeId).Build();
    }

    private void SetupPlayerFull(PlayerDto player, int clubsCount = 0)
    {
        var clubs = Enumerable.Range(1, clubsCount)
            .Select(i => ClubDtoBuilder.Valid().WithId((ulong)i).WithName("Club" + i).Build())
            .ToList();

        _playerHandler.Setup(_ => _.GetPlayerFullInfoAsync(It.IsAny<PlayerDto>()))
            .ReturnsAsync(new PlayerFullDto
            {
                Player = player,
                Clubs = clubs,
                PlayerClubs = clubs
                    .Select((c, i) => new PlayerClubDto { PlayerId = 1, ClubId = c.Id, HistoryPosition = (byte)(i + 1) })
                    .ToList()
            });
    }

    /// <summary>Trouve le jour meme (IsCurrentDay), avec un score et une heure donnes.</summary>
    private static LeaderDto Leader(ushort points, int minutes, bool sameDay = true)
    {
        return LeaderDtoBuilder.Valid().WithUserId(UserId).WithPoints(points).WithTime(minutes).WithProposalDate(Day).WithCreationDate(sameDay ? Day.AddMinutes(minutes) : Day.AddDays(3)).Build();
    }

    private static ProposalDto Proposal(ProposalTypes type, bool successful)
    {
        return ProposalDtoBuilder.Valid().WithUser(UserId).WithProposalTypeId((ulong)type).WithSuccessfulFlag((byte)(successful ? 1 : 0)).WithValue("x").WithProposalDate(Day).WithCreationDate(Day.AddMinutes(1)).Build();
    }

    private async Task Run(LeaderDto leader, PlayerDto player, params ProposalDto[] proposals)
    {
        SetupPlayerFull(player);

        // en production la ligne "leaders" est ecrite avant le calcul des badges
        // (ProposalService), l'historique du jour contient donc toujours le leader
        _leaderRepository
            .Setup(_ => _.GetLeadersAtDateAsync(leader.ProposalDate, It.IsAny<bool>()))
            .ReturnsAsync(new List<LeaderDto> { leader });

        await _service
            .PrepareNewLeaderBadgesAsync(leader, player, proposals.ToList(), Languages.en);
    }

    private void ShouldHaveGranted(params Badges[] badges)
    {
        _inserted.Select(_ => _.BadgeId)
            .Should().Contain(badges.Select(b => (ulong)b));
    }

    private void ShouldNotHaveGranted(params Badges[] badges)
    {
        _inserted.Select(_ => _.BadgeId)
            .Should().NotContain(badges.Select(b => (ulong)b));
    }

    /// <summary>
    /// Gain du jour precede d'un historique de joueurs deja trouves les jours d'avant
    /// (utilise par les badges bases sur <c>PlayersHistoryBasedBadgeCondition</c>, qui
    /// regardent tout l'historique, pas seulement le jour meme). Le jour du gain est
    /// decale de <paramref name="pastFinds"/>.Count + 1 jours apres <see cref="Day"/> —
    /// qui reste <c>FirstDate</c> — pour laisser de la place a un historique passe, sans
    /// quoi la fenetre [FirstDate, gain] ne contiendrait que le jour du gain lui-meme.
    /// </summary>
    private async Task RunWithPastFinds(PlayerDto todayPlayer, IReadOnlyList<PlayerDto> pastFinds)
    {
        var winDay = Day.AddDays(pastFinds.Count + 1);
        var leader = LeaderDtoBuilder.Valid().WithUserId(UserId).WithProposalDate(winDay).WithCreationDate(winDay.AddMinutes(60)).WithPoints(1000).WithTime(60).Build();
        var player = todayPlayer with { PublicationDate = winDay };

        SetupPlayerFull(player);

        _leaderRepository.Setup(_ => _.GetLeadersAtDateAsync(winDay, It.IsAny<bool>()))
            .ReturnsAsync(new List<LeaderDto> { leader });

        for (var i = 0; i < pastFinds.Count; i++)
        {
            var pastDate = winDay.AddDays(-(i + 1));
            _leaderRepository.Setup(_ => _.GetLeadersAtDateAsync(pastDate, It.IsAny<bool>()))
                .ReturnsAsync(new List<LeaderDto>
                {
                    LeaderDtoBuilder.Valid().WithUserId(UserId).WithProposalDate(pastDate).WithCreationDate(pastDate).Build()
                });
        }

        var pastFindsWithDates = pastFinds
            .Select((p, i) => p with { PublicationDate = winDay.AddDays(-(i + 1)) })
            .ToList();
        _playerRepository.Setup(_ => _.GetPlayersOfTheDayAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(pastFindsWithDates.Append(player).ToList());

        await _service.PrepareNewLeaderBadgesAsync(leader, player, [], Languages.en);
    }

    // ------------------------------------------------------------- badges de score

    [Fact]
    public async Task FindingThePlayerGrantsTheFirstSuccessBadge()
    {
        await Run(Leader(1000, 60), Player());

        ShouldHaveGranted(Badges.YourFirstSuccess);
    }

    [Theory]
    [InlineData((ushort)499, false, false)]
    [InlineData((ushort)500, true, false)]
    [InlineData((ushort)899, true, false)]
    [InlineData((ushort)900, true, true)]
    public async Task ScoreThresholdsAreInclusive(ushort points, bool halfway, bool over900)
    {
        await Run(Leader(points, 60), Player());

        _inserted.Any(_ => _.BadgeId == (ulong)Badges.HalfwayToTheTop).Should().Be(halfway);
        _inserted.Any(_ => _.BadgeId == (ulong)Badges.ItsOver900).Should().Be(over900);
    }

    [Fact]
    public async Task FindingWithZeroPointsGrantsTheWoodenSpoon()
    {
        await Run(Leader(0, 60), Player());

        ShouldHaveGranted(Badges.WoodenSpoon);
        ShouldNotHaveGranted(Badges.HalfwayToTheTop);
    }

    // ------------------------------------------------------------- badges horaires

    [Theory]
    [InlineData(0, Badges.StayUpLate)]      // minuit
    [InlineData(119, Badges.StayUpLate)]    // 01h59
    [InlineData(330, Badges.CacaCaféClopeKikolé)]   // 05h30
    [InlineData(479, Badges.CacaCaféClopeKikolé)]   // 07h59
    [InlineData(1400, Badges.SavedByTheBell)]       // 23h20
    public async Task TimeOfDayBadgesUseTheMinutesSinceMidnight(int minutes, Badges expected)
    {
        await Run(Leader(1000, minutes), Player());

        ShouldHaveGranted(expected);
    }

    [Theory]
    [InlineData(120)]   // 02h00, juste apres StayUpLate
    [InlineData(480)]   // 08h00, juste apres le creneau cafe
    [InlineData(1379)]  // 22h59, juste avant SavedByTheBell
    public async Task TimeOfDayBadgesHaveExclusiveUpperBounds(int minutes)
    {
        await Run(Leader(1000, minutes), Player());

        ShouldNotHaveGranted(
            Badges.StayUpLate, Badges.CacaCaféClopeKikolé, Badges.SavedByTheBell);
    }

    // ------------------------------------------------------------- badges lies au joueur

    [Theory]
    [InlineData((ushort)1969, true, false)]
    [InlineData((ushort)1939, true, true)]
    [InlineData((ushort)1970, false, false)]
    public async Task BirthYearBadgesAreCumulative(ushort year, bool archaeology, bool worldWarTwo)
    {
        await Run(Leader(1000, 60), Player(year));

        _inserted.Any(_ => _.BadgeId == (ulong)Badges.Archaeology).Should().Be(archaeology);
        _inserted.Any(_ => _.BadgeId == (ulong)Badges.WorldWarTwo).Should().Be(worldWarTwo);
    }

    [Fact]
    public async Task APlayerCanCarryASpecialBadge()
    {
        await Run(Leader(1000, 60), Player(badgeId: (ulong)Badges.LegendTier));

        ShouldHaveGranted(Badges.LegendTier);
    }

    // ------------------------------------------------------------- badges lies aux propositions

    [Fact]
    public async Task FindingWithoutAnyPreviousProposalGrantsImFeelingLucky()
    {
        await Run(Leader(1000, 60), Player());

        ShouldHaveGranted(Badges.ImFeelingLucky);
    }

    [Fact]
    public async Task AtLeastOneProposalRemovesImFeelingLucky()
    {
        await Run(Leader(950, 60), Player(), Proposal(ProposalTypes.Club, false));

        ShouldNotHaveGranted(Badges.ImFeelingLucky);
    }

    [Fact]
    public async Task OnlyClubProposalsGrantWikipediaScreenshot()
    {
        await Run(Leader(900, 60), Player(),
                Proposal(ProposalTypes.Club, true),
                Proposal(ProposalTypes.Club, false));

        ShouldHaveGranted(Badges.WikipediaScreenshot);
        ShouldNotHaveGranted(Badges.PassportCheck);
    }

    [Fact]
    public async Task NoClubProposalAtAllGrantsPassportCheck()
    {
        await Run(Leader(900, 60), Player(),
                Proposal(ProposalTypes.Country, true),
                Proposal(ProposalTypes.Year, true));

        ShouldHaveGranted(Badges.PassportCheck);
        ShouldNotHaveGranted(Badges.WikipediaScreenshot);
    }

    [Fact]
    public async Task FailingEveryPreviousProposalGrantsEverythingNotLost()
    {
        await Run(Leader(500, 60), Player(),
                Proposal(ProposalTypes.Country, false),
                Proposal(ProposalTypes.Year, false));

        ShouldHaveGranted(Badges.EverythingNotLost);
    }

    [Fact]
    public async Task ASingleSuccessfulProposalRemovesEverythingNotLost()
    {
        await Run(Leader(500, 60), Player(),
                Proposal(ProposalTypes.Country, false),
                Proposal(ProposalTypes.Year, true));

        ShouldNotHaveGranted(Badges.EverythingNotLost);
    }

    // ------------------------------------------------------------- garde-fous

    [Fact]
    public async Task FindingOnALaterDayForfeitsTheSameDayBadges()
    {
        // seuls les badges lies au joueur restent accessibles en rattrapage
        await Run(Leader(1000, 60, sameDay: false), Player(1939));

        ShouldNotHaveGranted(
            Badges.YourFirstSuccess, Badges.ItsOver900, Badges.ImFeelingLucky);
        ShouldHaveGranted(Badges.Archaeology, Badges.WorldWarTwo);
    }

    [Fact]
    public async Task ABadgeAlreadyOwnedIsNotGrantedTwice()
    {
        _badgeRepository
            .Setup(_ => _.CheckUserHasBadgeAsync(UserId, (ulong)Badges.YourFirstSuccess))
            .ReturnsAsync(true);

        await Run(Leader(1000, 60), Player());

        ShouldNotHaveGranted(Badges.YourFirstSuccess);
        ShouldHaveGranted(Badges.ItsOver900);
    }

    [Fact]
    public async Task ABadgeCreatedAfterTheDayIsNotGrantedRetroactively()
    {
        _badgeRepository.Setup(_ => _.GetBadgesAsync(It.IsAny<bool>()))
            .ReturnsAsync(Enum.GetValues(typeof(Badges)).Cast<Badges>()
                .Select(b => new BadgeDto { Id = (ulong)b,
                    Name = b.ToString(),
                    Description = b.ToString(),
                    // cree demain : aucune journee passee ne peut l'obtenir
                    CreationDate = Day.AddDays(1) }).ToList());

        await Run(Leader(1000, 60), Player());

        _inserted.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryGrantedBadgeIsStampedWithTheProposalDateAndUser()
    {
        await Run(Leader(1000, 60), Player());

        _inserted.Should().NotBeEmpty();
        _inserted.Should().OnlyContain(_ => _.UserId == UserId && _.GetDate == Day);
    }

    // ------------------------------------------------------------- historique des joueurs trouves

    [Fact]
    public async Task EnoughPlayersInEveryPositionGrantsFourFourTwo()
    {
        // le gain du jour compte deja 1 milieu (Player() par defaut) : il manque donc
        // 1 gardien, 4 defenseurs, 3 milieux et 2 attaquants pour depasser les seuils
        var pastFinds = new List<PlayerDto>
        {
            Player() with { PositionId = (ulong)Positions.Goalkeeper },
            Player() with { PositionId = (ulong)Positions.Defender }, Player() with { PositionId = (ulong)Positions.Defender },
            Player() with { PositionId = (ulong)Positions.Defender }, Player() with { PositionId = (ulong)Positions.Defender },
            Player() with { PositionId = (ulong)Positions.Midfielder }, Player() with { PositionId = (ulong)Positions.Midfielder },
            Player() with { PositionId = (ulong)Positions.Midfielder },
            Player() with { PositionId = (ulong)Positions.Forward }, Player() with { PositionId = (ulong)Positions.Forward }
        };

        await RunWithPastFinds(Player(), pastFinds);

        ShouldHaveGranted(Badges.FourFourtwo);
    }

    [Fact]
    public async Task NotEnoughDefendersDoesNotGrantFourFourTwo()
    {
        var pastFinds = new List<PlayerDto>
        {
            Player() with { PositionId = (ulong)Positions.Goalkeeper },
            Player() with { PositionId = (ulong)Positions.Defender }, Player() with { PositionId = (ulong)Positions.Defender },
            Player() with { PositionId = (ulong)Positions.Defender }, // seulement 3 defenseurs au total
            Player() with { PositionId = (ulong)Positions.Midfielder }, Player() with { PositionId = (ulong)Positions.Midfielder },
            Player() with { PositionId = (ulong)Positions.Midfielder },
            Player() with { PositionId = (ulong)Positions.Forward }, Player() with { PositionId = (ulong)Positions.Forward }
        };

        await RunWithPastFinds(Player(), pastFinds);

        ShouldNotHaveGranted(Badges.FourFourtwo);
    }

    [Fact]
    public async Task TwentyDistinctCountriesGrantsAroundTheWorld()
    {
        // le gain du jour compte deja la France : il en faut 19 de plus
        var pastFinds = Enumerable.Range(1, 19)
            .Select(i => Player() with { CountryId = 1000 + (ulong)i })
            .ToList();

        await RunWithPastFinds(Player(), pastFinds);

        ShouldHaveGranted(Badges.AroundTheWorld);
    }

    [Fact]
    public async Task NineteenDistinctCountriesDoesNotGrantAroundTheWorld()
    {
        var pastFinds = Enumerable.Range(1, 18)
            .Select(i => Player() with { CountryId = 2000 + (ulong)i })
            .ToList();

        await RunWithPastFinds(Player(), pastFinds);

        ShouldNotHaveGranted(Badges.AroundTheWorld);
    }

    // ------------------------------------------------------------- OneMinuteChrono

    private async Task RunChrono(IReadOnlyList<ProposalDto> proposals, int clubsCount = 5)
    {
        var player = Player();
        SetupPlayerFull(player, clubsCount);

        var leader = Leader(1000, 60);
        _leaderRepository.Setup(_ => _.GetLeadersAtDateAsync(leader.ProposalDate, It.IsAny<bool>()))
            .ReturnsAsync(new List<LeaderDto> { leader });

        await _service.PrepareNewLeaderBadgesAsync(leader, player, proposals, Languages.en);
    }

    private static List<ProposalDto> ChronoProposals(DateTime winTime, int secondsBeforeWin, int clubsCount = 5, bool includeClueRequest = false)
    {
        var start = winTime.AddSeconds(-secondsBeforeWin);
        var proposals = new List<ProposalDto>
        {
            ProposalDtoBuilder.Valid().WithUser(UserId).OfType(ProposalTypes.Year).Successful().WithCreationDate(start).Build(),
            ProposalDtoBuilder.Valid().WithUser(UserId).OfType(ProposalTypes.Position).Successful().WithCreationDate(start.AddSeconds(1)).Build(),
            ProposalDtoBuilder.Valid().WithUser(UserId).OfType(ProposalTypes.Country).Successful().WithCreationDate(start.AddSeconds(2)).Build()
        };
        for (var i = 0; i < clubsCount; i++)
            proposals.Add(ProposalDtoBuilder.Valid().WithUser(UserId).OfType(ProposalTypes.Club).Successful().WithCreationDate(start.AddSeconds(3 + i)).Build());
        if (includeClueRequest)
            proposals.Add(ProposalDtoBuilder.Valid().WithUser(UserId).OfType(ProposalTypes.Clue).Successful().WithCreationDate(start).Build());
        return proposals;
    }

    [Fact]
    public async Task AllCategoriesUnderAMinuteGrantsOneMinuteChrono()
    {
        var winTime = Day.AddMinutes(60);

        await RunChrono(ChronoProposals(winTime, secondsBeforeWin: 45));

        ShouldHaveGranted(Badges.OneMinuteChrono);
    }

    [Fact]
    public async Task MoreThanAMinuteDoesNotGrantOneMinuteChrono()
    {
        var winTime = Day.AddMinutes(60);

        await RunChrono(ChronoProposals(winTime, secondsBeforeWin: 90));

        ShouldNotHaveGranted(Badges.OneMinuteChrono);
    }

    [Fact]
    public async Task AMissingCategoryDoesNotGrantOneMinuteChrono()
    {
        var winTime = Day.AddMinutes(60);
        var proposals = ChronoProposals(winTime, secondsBeforeWin: 45)
            .Where(p => (ProposalTypes)p.ProposalTypeId != ProposalTypes.Position)
            .ToList();

        await RunChrono(proposals);

        ShouldNotHaveGranted(Badges.OneMinuteChrono);
    }

    [Fact]
    public async Task RequestingTheClueDoesNotGrantOneMinuteChrono()
    {
        var winTime = Day.AddMinutes(60);

        await RunChrono(ChronoProposals(winTime, secondsBeforeWin: 45, includeClueRequest: true));

        ShouldNotHaveGranted(Badges.OneMinuteChrono);
    }

    [Fact]
    public async Task FewerClubProposalsThanCareerLengthDoesNotGrantOneMinuteChrono()
    {
        var winTime = Day.AddMinutes(60);

        // carriere de 5 clubs (RunChrono par defaut) mais seulement 4 clubs proposes
        await RunChrono(ChronoProposals(winTime, secondsBeforeWin: 45, clubsCount: 4));

        ShouldNotHaveGranted(Badges.OneMinuteChrono);
    }

    // ------------------------------------------------------------- Dedicated (PrepareNonLeaderBadgesAsync)

    private static ProposalRequest TodayPlayerRequest()
    {
        return new ProposalRequest
        {
            Value = "x",
            ProposalType = ProposalTypes.Club,
            ProposalDateTime = Day,
            DaysBeforeNow = 0
        };
    }

    [Fact]
    public async Task ThirtyDayStreakOfActivityGrantsDedicated()
    {
        var proposals = Enumerable.Range(1, 29)
            .Select(i => ProposalDtoBuilder.Valid().WithUser(UserId).WithProposalDate(Day.AddDays(-i)).Build())
            .ToList();
        _proposalRepository.Setup(_ => _.GetAllProposalsDateExactAsync(UserId)).ReturnsAsync(proposals);
        _playerRepository.Setup(_ => _.GetPlayersByCreatorAsync(UserId, true)).ReturnsAsync(new List<PlayerDto>());

        await _service.PrepareNonLeaderBadgesAsync(UserId, TodayPlayerRequest(), Languages.en);

        ShouldHaveGranted(Badges.Dedicated);
    }

    [Fact]
    public async Task AGapInTheStreakDoesNotGrantDedicated()
    {
        var proposals = Enumerable.Range(1, 29)
            .Where(i => i != 15) // trou au 15e jour precedent
            .Select(i => ProposalDtoBuilder.Valid().WithUser(UserId).WithProposalDate(Day.AddDays(-i)).Build())
            .ToList();
        _proposalRepository.Setup(_ => _.GetAllProposalsDateExactAsync(UserId)).ReturnsAsync(proposals);
        _playerRepository.Setup(_ => _.GetPlayersByCreatorAsync(UserId, true)).ReturnsAsync(new List<PlayerDto>());

        await _service.PrepareNonLeaderBadgesAsync(UserId, TodayPlayerRequest(), Languages.en);

        ShouldNotHaveGranted(Badges.Dedicated);
    }

    [Fact]
    public async Task CreatingAPublishedPlayerCountsAsActivityForDedicated()
    {
        // jour 15 couvert par la creation d'un joueur publie plutot qu'une proposition
        var proposals = Enumerable.Range(1, 29)
            .Where(i => i != 15)
            .Select(i => ProposalDtoBuilder.Valid().WithUser(UserId).WithProposalDate(Day.AddDays(-i)).Build())
            .ToList();
        _proposalRepository.Setup(_ => _.GetAllProposalsDateExactAsync(UserId)).ReturnsAsync(proposals);
        _playerRepository.Setup(_ => _.GetPlayersByCreatorAsync(UserId, true))
            .ReturnsAsync(new List<PlayerDto> { Player() with { PublicationDate = Day.AddDays(-15) } });

        await _service.PrepareNonLeaderBadgesAsync(UserId, TodayPlayerRequest(), Languages.en);

        ShouldHaveGranted(Badges.Dedicated);
    }

    [Fact]
    public async Task NotBeingTodaysPlayerNeverGrantsDedicated()
    {
        var request = TodayPlayerRequest() with { DaysBeforeNow = 1 };

        await _service.PrepareNonLeaderBadgesAsync(UserId, request, Languages.en);

        ShouldNotHaveGranted(Badges.Dedicated);
        _proposalRepository.Verify(_ => _.GetAllProposalsDateExactAsync(It.IsAny<ulong>()), Times.Never);
    }

    // ------------------------------------------------------------- visibilite (GetUserBadgesAsync)

    private void SetupHiddenBadge(DateTime obtainedOn)
    {
        _badgeRepository.Setup(_ => _.GetBadgesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<BadgeDto>
            {
                BadgeDtoBuilder.Valid().WithId((ulong)Badges.YourFirstSuccess).WithName("Secret").WithDescription("Secret").Hidden().WithCreationDate(Day.AddYears(-1)).Build()
            });
        _badgeRepository.Setup(_ => _.GetUserBadgesAsync(UserId))
            .ReturnsAsync(new List<UserBadgeDto>
            {
                new() { UserId = UserId, BadgeId = (ulong)Badges.YourFirstSuccess, GetDate = obtainedOn }
            });
    }

    [Fact]
    public async Task OwnerCanSeeTheirOwnHiddenBadgeEarnedToday()
    {
        SetupHiddenBadge(Day);

        var result = await _service.GetUserBadgesAsync(UserId, UserId, Languages.en, foundToday: true);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task AnotherStandardUserCannotSeeAHiddenBadgeEarnedToday()
    {
        SetupHiddenBadge(Day);
        _userRepository.Setup(_ => _.GetUserByIdAsync(It.IsAny<ulong>()))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(999).WithType(UserTypes.StandardUser).Build());

        var result = await _service.GetUserBadgesAsync(UserId, 999, Languages.en, foundToday: true);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnAdministratorCanSeeSomeoneElsesHiddenBadgeEarnedToday()
    {
        SetupHiddenBadge(Day);
        _userRepository.Setup(_ => _.GetUserByIdAsync(It.IsAny<ulong>()))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(999).WithType(UserTypes.Administrator).Build());

        var result = await _service.GetUserBadgesAsync(UserId, 999, Languages.en, foundToday: true);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task AHiddenBadgeEarnedYesterdayIsVisibleToEveryone()
    {
        SetupHiddenBadge(Day.AddDays(-1));
        _userRepository.Setup(_ => _.GetUserByIdAsync(It.IsAny<ulong>()))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(999).WithType(UserTypes.StandardUser).Build());

        var result = await _service.GetUserBadgesAsync(UserId, 999, Languages.en, foundToday: true);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task FoundTodayFalseExcludesBadgesEarnedToday()
    {
        _badgeRepository.Setup(_ => _.GetBadgesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<BadgeDto>
            {
                BadgeDtoBuilder.Valid().WithId((ulong)Badges.YourFirstSuccess).WithCreationDate(Day.AddYears(-1)).Build()
            });
        _badgeRepository.Setup(_ => _.GetUserBadgesAsync(UserId))
            .ReturnsAsync(new List<UserBadgeDto>
            {
                new() { UserId = UserId, BadgeId = (ulong)Badges.YourFirstSuccess, GetDate = Day }
            });

        var result = await _service.GetUserBadgesAsync(UserId, UserId, Languages.en, foundToday: false);

        result.Should().BeEmpty();
    }

    // ------------------------------------------------------------- AddBadgeToUserAsync

    [Fact]
    public async Task AddBadgeToUserAsync_GrantsTheBadgeWhenNotAlreadyOwned()
    {
        var granted = await _service.AddBadgeToUserAsync(Badges.YourFirstSuccess, UserId);

        granted.Should().BeTrue();
        ShouldHaveGranted(Badges.YourFirstSuccess);
    }

    [Fact]
    public async Task AddBadgeToUserAsync_DoesNothingWhenAlreadyOwned()
    {
        _badgeRepository.Setup(_ => _.CheckUserHasBadgeAsync(UserId, (ulong)Badges.YourFirstSuccess))
            .ReturnsAsync(true);

        var granted = await _service.AddBadgeToUserAsync(Badges.YourFirstSuccess, UserId);

        granted.Should().BeFalse();
        ShouldNotHaveGranted(Badges.YourFirstSuccess);
    }

    // ------------------------------------------------------------- GetAllBadgesAsync

    [Fact]
    public async Task GetAllBadgesAsync_OrdersByUsersCountDescending()
    {
        _badgeRepository.Setup(_ => _.GetBadgesAsync(false))
            .ReturnsAsync(new List<BadgeDto>
            {
                BadgeDtoBuilder.Valid().WithId(1).WithName("A").WithDescription("dA").Build(),
                BadgeDtoBuilder.Valid().WithId(2).WithName("B").WithDescription("dB").Build()
            });
        _badgeRepository.Setup(_ => _.GetUsersWithBadgeAsync(1)).ReturnsAsync(new List<UserBadgeDto> { new(), new() });
        _badgeRepository.Setup(_ => _.GetUsersWithBadgeAsync(2)).ReturnsAsync(new List<UserBadgeDto> { new() });

        var result = await _service.GetAllBadgesAsync(Languages.en);

        result.Select(b => b.Id).Should().ContainInOrder(1UL, 2UL);
    }

    [Fact]
    public async Task GetAllBadgesAsync_UsesTheTranslatedDescriptionWhenNotEnglish()
    {
        _badgeRepository.Setup(_ => _.GetBadgesAsync(false))
            .ReturnsAsync(new List<BadgeDto>
            {
                BadgeDtoBuilder.Valid().WithId(1).WithName("A").WithDescription("English description").Build()
            });
        _badgeRepository.Setup(_ => _.GetUsersWithBadgeAsync(1)).ReturnsAsync(new List<UserBadgeDto>());
        _badgeRepository.Setup(_ => _.GetBadgeDescriptionAsync(1, (ulong)Languages.fr))
            .ReturnsAsync("Description en francais");

        var result = await _service.GetAllBadgesAsync(Languages.fr);

        result.Should().ContainSingle().Which.Description.Should().Be("Description en francais");
    }

    [Fact]
    public async Task GetAllBadgesAsync_FallsBackToTheDefaultDescriptionWhenNoTranslationExists()
    {
        _badgeRepository.Setup(_ => _.GetBadgesAsync(false))
            .ReturnsAsync(new List<BadgeDto>
            {
                BadgeDtoBuilder.Valid().WithId(1).WithName("A").WithDescription("English description").Build()
            });
        _badgeRepository.Setup(_ => _.GetUsersWithBadgeAsync(1)).ReturnsAsync(new List<UserBadgeDto>());
        _badgeRepository.Setup(_ => _.GetBadgeDescriptionAsync(1, (ulong)Languages.fr))
            .ReturnsAsync((string?)null);

        var result = await _service.GetAllBadgesAsync(Languages.fr);

        result.Should().ContainSingle().Which.Description.Should().Be("English description");
    }

    // ------------------------------------------------------------- ResetBadgesAsync

    [Fact]
    public async Task ResetBadgesAsync_ClearsOnlyRecomputableBadges()
    {
        var nonRecomputable = new[]
        {
            Badges.DoItYourself, Badges.WeAreKikole, Badges.Dedicated
        };

        _playerRepository.Setup(_ => _.GetPlayersOfTheDayAsync(TestCalendar.HiddenDate, Day))
            .ReturnsAsync(new List<PlayerDto>());

        await _service.ResetBadgesAsync(Languages.en);

        foreach (var badge in nonRecomputable)
        {
            _badgeRepository.Verify(
                _ => _.ResetBadgeDatasAsync((ulong)badge), Times.Never,
                $"{badge} est attribue manuellement et ne doit jamais etre efface");
        }

        _badgeRepository.Verify(
            _ => _.ResetBadgeDatasAsync((ulong)Badges.YourFirstSuccess), Times.Once);
    }
}
