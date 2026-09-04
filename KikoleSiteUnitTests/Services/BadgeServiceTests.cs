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
