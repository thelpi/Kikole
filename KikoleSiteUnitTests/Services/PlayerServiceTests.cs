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

public class PlayerServiceTests
{
    // les dates sont exprimees relativement a FirstDate pour que les tests
    // survivent au changement de cette constante
    private static readonly DateTime FirstDate = TestCalendar.FirstDate;

    private readonly Mock<IPlayerHandler> _playerHandler = new();
    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILeaderRepository> _leaderRepository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly PlayerService _service;

    public PlayerServiceTests()
    {
        _clock.Setup(_ => _.Today).Returns(FirstDate);
        _clock.Setup(_ => _.Tomorrow).Returns(FirstDate.AddDays(1));

        _service = new PlayerService(
            _playerHandler.Object,
            _playerRepository.Object,
            _userRepository.Object,
            _leaderRepository.Object,
            _clock.Object,
            _gameCalendar.Object,
            new Random(1));
    }

    private static PlayerRequest Request()
    {
        return new PlayerRequest
        {
            Name = "Zinédine Zidane",
            YearOfBirth = 1972,
            Country = Countries.FRA,
            Position = Positions.Midfielder,
            AllowedNames = new List<string> { "Zidane" },
            Clubs = new List<PlayerClubRequest>
            {
                new() { ClubId = 1, HistoryPosition = 1 },
                new() { ClubId = 2, HistoryPosition = 2 }
            },
            ClueLanguages = new Dictionary<Languages, string?>(),
            EasyClueLanguages = new Dictionary<Languages, string?>(),
            ClueEn = "clue",
            EasyClueEn = "easy clue"
        };
    }

    // ------------------------------------------------------------- CreatePlayerAsync

    [Fact]
    public async Task CreatePlayerAsync_WhenAskedForTheNextSlot_TakesTheDayAfterTheLatestOne()
    {
        var request = Request() with { SetLatestPublicationDate = true };
        _playerRepository.Setup(_ => _.GetLatestPlayerDateAsync())
            .ReturnsAsync(FirstDate.AddDays(4));
        _playerRepository.Setup(_ => _.CreatePlayerAsync(It.IsAny<PlayerDto>())).ReturnsAsync(9UL);

        await _service.CreatePlayerAsync(request, 42);

        _playerRepository.Verify(
            _ => _.CreatePlayerAsync(It.Is<PlayerDto>(d => d.PublicationDate == FirstDate.AddDays(5))),
            Times.Once);
    }

    [Fact]
    public async Task CreatePlayerAsync_AnExplicitDateWins()
    {
        var request = Request() with { SetLatestPublicationDate = true, PublicationDate = FirstDate.AddDays(10) };
        _playerRepository.Setup(_ => _.CreatePlayerAsync(It.IsAny<PlayerDto>())).ReturnsAsync(9UL);

        await _service.CreatePlayerAsync(request, 42);

        _playerRepository.Verify(_ => _.GetLatestPlayerDateAsync(), Times.Never);
        _playerRepository.Verify(
            _ => _.CreatePlayerAsync(It.Is<PlayerDto>(d => d.PublicationDate == FirstDate.AddDays(10))),
            Times.Once);
    }

    [Fact]
    public async Task CreatePlayerAsync_WithoutAnyDate_StaysPendingValidation()
    {
        var request = Request();
        _playerRepository.Setup(_ => _.CreatePlayerAsync(It.IsAny<PlayerDto>())).ReturnsAsync(9UL);

        await _service.CreatePlayerAsync(request, 42);

        _playerRepository.Verify(
            _ => _.CreatePlayerAsync(It.Is<PlayerDto>(d => d.PublicationDate == null)),
            Times.Once);
    }

    [Fact]
    public async Task CreatePlayerAsync_CreatesEveryCareerEntry()
    {
        _playerRepository.Setup(_ => _.CreatePlayerAsync(It.IsAny<PlayerDto>())).ReturnsAsync(9UL);

        await _service.CreatePlayerAsync(Request(), 42);

        _playerRepository.Verify(
            _ => _.CreatePlayerClubsAsync(It.Is<PlayerClubDto>(c => c.PlayerId == 9)),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CreatePlayerAsync_SkipsBlankTranslationsAndTrimsTheOthers()
    {
        var request = Request() with { ClueLanguages = new Dictionary<Languages, string?>
        {
            { Languages.fr, "  indice francais  " },
            { Languages.en, "   " }
        } };
        _playerRepository.Setup(_ => _.CreatePlayerAsync(It.IsAny<PlayerDto>())).ReturnsAsync(9UL);

        await _service.CreatePlayerAsync(request, 42);

        _playerRepository.Verify(
            _ => _.InsertPlayerCluesByLanguageAsync(9, 0,
                It.Is<IReadOnlyDictionary<ulong, string>>(d =>
                    d.Count == 1 && d[(ulong)Languages.fr] == "indice francais")),
            Times.Once);
    }

    [Fact]
    public async Task CreatePlayerAsync_WhenNoTranslationIsProvided_NothingIsInserted()
    {
        _playerRepository.Setup(_ => _.CreatePlayerAsync(It.IsAny<PlayerDto>())).ReturnsAsync(9UL);

        await _service.CreatePlayerAsync(Request(), 42);

        _playerRepository.Verify(
            _ => _.InsertPlayerCluesByLanguageAsync(
                It.IsAny<ulong>(), It.IsAny<byte>(), It.IsAny<IReadOnlyDictionary<ulong, string>>()),
            Times.Never);
    }

    // ------------------------------------------------------------- GetPlayerClueAsync

    [Theory]
    [InlineData(false, "the clue")]
    [InlineData(true, "the easy clue")]
    public async Task GetPlayerClueAsync_InEnglish_ReadsThePlayerRowDirectly(bool isEasy, string expected)
    {
        _playerRepository.Setup(_ => _.GetPlayerOfTheDayAsync(FirstDate))
            .ReturnsAsync(PlayerDtoBuilder.Valid().WithId(1).WithClue("the clue").WithEasyClue("the easy clue").Build());

        var result = await _service
            .GetPlayerClueAsync(FirstDate, isEasy, Languages.en);

        result.Should().Be(expected);
        _playerRepository.Verify(
            _ => _.GetClueAsync(It.IsAny<ulong>(), It.IsAny<byte>(), It.IsAny<ulong>()),
            Times.Never);
    }

    [Theory]
    [InlineData(false, (byte)0)]
    [InlineData(true, (byte)1)]
    public async Task GetPlayerClueAsync_InAnotherLanguage_ReadsTheTranslation(bool isEasy, byte expectedFlag)
    {
        _playerRepository.Setup(_ => _.GetPlayerOfTheDayAsync(FirstDate))
            .ReturnsAsync(PlayerDtoBuilder.Valid().WithId(1).WithClue("the clue").WithEasyClue("the easy clue").Build());
        _playerRepository.Setup(_ => _.GetClueAsync(1, expectedFlag, (ulong)Languages.fr))
            .ReturnsAsync("indice traduit");

        var result = await _service
            .GetPlayerClueAsync(FirstDate, isEasy, Languages.fr);

        result.Should().Be("indice traduit");
    }

    // ------------------------------------------------------------- invariantes

    // Un joueur par jour est une regle du jeu, pas un cas a degrader : ces appels
    // echouent bruyamment plutot que de casser plus loin sur une reference nulle,
    // et le message nomme ce qui manque pour que l'administrateur puisse corriger.

    [Fact]
    public async Task GetPlayerClueAsync_WhenNoPlayerForThatDay_SaysWhichDayIsMissing()
    {
        _playerRepository.Setup(_ => _.GetPlayerOfTheDayAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((PlayerDto?)null);

        Func<Task> act = () => _service.GetPlayerClueAsync(FirstDate, false, Languages.en);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{FirstDate:yyyy-MM-dd}*");
    }

    [Fact]
    public async Task GetPlayerCluesAsync_WhenThePlayerDoesNotExist_SaysWhichPlayerIsMissing()
    {
        _playerRepository.Setup(_ => _.GetPlayerByIdAsync(It.IsAny<ulong>()))
            .ReturnsAsync((PlayerDto?)null);

        Func<Task> act = () => _service.GetPlayerCluesAsync(42, new[] { Languages.en });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*42*");
    }

    // ------------------------------------------------------------- validation d'une soumission

    [Fact]
    public async Task ValidatePlayerSubmissionAsync_WhenThePlayerDoesNotExist_ReportsNotFound()
    {
        _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1)).ReturnsAsync((PlayerDto?)null);

        var (error, userId, badges) = await _service
            .ValidatePlayerSubmissionAsync(PlayerSubmissionValidationRequestBuilder.Valid().Build());

        error.Should().Be(PlayerSubmissionErrors.PlayerNotFound);
        userId.Should().Be(0);
        badges.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidatePlayerSubmissionAsync_WhenAlreadyScheduled_IsRefused()
    {
        _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
            .ReturnsAsync(PlayerDtoBuilder.Valid().WithId(1).WithPublicationDate(FirstDate).Build());

        var (error, _, _) = await _service
            .ValidatePlayerSubmissionAsync(PlayerSubmissionValidationRequestBuilder.Valid().Build());

        error.Should().Be(PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused);
    }

    [Fact]
    public async Task ValidatePlayerSubmissionAsync_WhenAlreadyRefused_IsRefusedAgain()
    {
        _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
            .ReturnsAsync(PlayerDtoBuilder.Valid().WithId(1).WithRejectDate(FirstDate).Build());

        var (error, _, _) = await _service
            .ValidatePlayerSubmissionAsync(PlayerSubmissionValidationRequestBuilder.Valid().Build());

        error.Should().Be(PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused);
    }

    private void SetupPendingPlayer(int acceptedPlayersOfCreator)
    {
        _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
            .ReturnsAsync(PlayerDtoBuilder.Valid().WithId(1).WithCreator(42).WithClue("current clue").WithEasyClue("current easy clue").Build());
        _playerRepository.Setup(_ => _.GetLatestPlayerDateAsync()).ReturnsAsync(FirstDate.AddDays(2));
        _playerRepository.Setup(_ => _.GetPlayersByCreatorAsync(42, true))
            .ReturnsAsync(Enumerable.Range(0, acceptedPlayersOfCreator)
                .Select(_ => PlayerDtoBuilder.Valid().Build()).ToList());
    }

    private static PlayerSubmissionValidationRequest Acceptance()
    {
        return PlayerSubmissionValidationRequestBuilder.Valid().Accepted("indice", "facile").Build();
    }

    [Fact]
    public async Task ValidatePlayerSubmissionAsync_WhenAccepted_SchedulesTheDayAfterTheLatestOne()
    {
        SetupPendingPlayer(1);

        var (error, userId, badges) = await _service
            .ValidatePlayerSubmissionAsync(Acceptance());

        error.Should().Be(PlayerSubmissionErrors.NoError);
        userId.Should().Be(42);
        badges.Should().Contain(Badges.DoItYourself);
        _playerRepository.Verify(
            _ => _.ValidatePlayerProposalAsync(1, FirstDate.AddDays(3)), Times.Once);
    }

    [Fact]
    public async Task ValidatePlayerSubmissionAsync_TheFifthAcceptedPlayerGrantsWeAreKikole()
    {
        SetupPendingPlayer(5);

        var (_, _, badges) = await _service
            .ValidatePlayerSubmissionAsync(Acceptance());

        badges.Should().BeEquivalentTo(new[] { Badges.DoItYourself, Badges.WeAreKikole });
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public async Task ValidatePlayerSubmissionAsync_WeAreKikoleIsGrantedOnlyOnTheExactFifth(int accepted)
    {
        // la comparaison est une egalite stricte : le badge est manque si le compteur
        // saute 5, et n'est jamais redonne ensuite
        SetupPendingPlayer(accepted);

        var (_, _, badges) = await _service
            .ValidatePlayerSubmissionAsync(Acceptance());

        badges.Should().NotContain(Badges.WeAreKikole);
    }

    [Fact]
    public async Task ValidatePlayerSubmissionAsync_WhenTheClueIsNotEdited_TheCurrentOneIsKept()
    {
        SetupPendingPlayer(1);

        await _service.ValidatePlayerSubmissionAsync(Acceptance());

        _playerRepository.Verify(
            _ => _.UpdatePlayerCluesAsync(1, "current clue", "current easy clue"), Times.Once);
    }

    [Fact]
    public async Task ValidatePlayerSubmissionAsync_AnEditedClueIsTrimmedAndOverrides()
    {
        SetupPendingPlayer(1);
        var request = Acceptance() with { ClueEditEn = "  nouvel indice  " };

        await _service.ValidatePlayerSubmissionAsync(request);

        _playerRepository.Verify(
            _ => _.UpdatePlayerCluesAsync(1, "nouvel indice", "current easy clue"), Times.Once);
    }

    [Fact]
    public async Task ValidatePlayerSubmissionAsync_WhenRefused_NoDateIsAssignedAndNoBadgeIsGranted()
    {
        _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
            .ReturnsAsync(PlayerDtoBuilder.Valid().WithId(1).WithCreator(42).Build());

        var request = PlayerSubmissionValidationRequestBuilder.Valid().Refused("doublon").Build();

        var (error, userId, badges) = await _service
            .ValidatePlayerSubmissionAsync(request);

        error.Should().Be(PlayerSubmissionErrors.NoError);
        userId.Should().Be(42);
        badges.Should().BeEmpty();
        _playerRepository.Verify(_ => _.RefusePlayerProposalAsync(1), Times.Once);
        _playerRepository.Verify(
            _ => _.ValidatePlayerProposalAsync(It.IsAny<ulong>(), It.IsAny<DateTime>()), Times.Never);
    }

    // ------------------------------------------------------------- ReassignPlayersOfTheDayAsync

    [Fact]
    public async Task ReassignPlayersOfTheDayAsync_WithinThirtyMinutesOfMidnight_DoesNothing()
    {
        // garde-fou : rebattre les cartes juste avant le changement de jour
        // pourrait changer le joueur du jour sous les pieds des joueurs
        _clock.Setup(_ => _.IsTomorrowIn(30)).Returns(true);

        await _service.ReassignPlayersOfTheDayAsync();

        _playerRepository.Verify(
            _ => _.ChangePlayerPublicationDateAsync(It.IsAny<ulong>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task ReassignPlayersOfTheDayAsync_SpreadsFuturePlayersOverConsecutiveDays()
    {
        _clock.Setup(_ => _.IsTomorrowIn(30)).Returns(false);
        _playerRepository.Setup(_ => _.GetPlayersOfTheDayAsync(FirstDate.AddDays(1), null))
            .ReturnsAsync(new List<PlayerDto>
            {
                PlayerDtoBuilder.Valid().WithId(1).Build(), PlayerDtoBuilder.Valid().WithId(2).Build(), PlayerDtoBuilder.Valid().WithId(3).Build()
            });

        var assigned = new List<DateTime>();
        _playerRepository
            .Setup(_ => _.ChangePlayerPublicationDateAsync(It.IsAny<ulong>(), It.IsAny<DateTime>()))
            .Callback<ulong, DateTime>((_, d) => assigned.Add(d))
            .Returns(Task.CompletedTask);

        await _service.ReassignPlayersOfTheDayAsync();

        assigned.Should().BeEquivalentTo(new[]
        {
            FirstDate.AddDays(1), FirstDate.AddDays(2), FirstDate.AddDays(3)
        });
    }

    // ------------------------------------------------------------- CanDisplayHiddenPlayerAsync

    private void SetupHiddenDay(int hiddenDayLeaders, int allLeaders, int createdPlayers, int daysSinceFirstDate)
    {
        _clock.Setup(_ => _.Today).Returns(FirstDate.AddDays(daysSinceFirstDate));
        _leaderRepository
            .Setup(_ => _.GetUserLeadersAsync(TestCalendar.HiddenDate, TestCalendar.HiddenDate, false, 7))
            .ReturnsAsync(Enumerable.Range(0, hiddenDayLeaders).Select(_ => LeaderDtoBuilder.Valid().Build()).ToList());
        _leaderRepository
            .Setup(_ => _.GetUserLeadersAsync(TestCalendar.FirstDate, null, false, 7))
            .ReturnsAsync(Enumerable.Range(0, allLeaders).Select(_ => LeaderDtoBuilder.Valid().Build()).ToList());
        _playerRepository
            .Setup(_ => _.GetPlayersByCreatorAsync(7, true))
            .ReturnsAsync(Enumerable.Range(0, createdPlayers)
                .Select(_ => PlayerDtoBuilder.Valid().WithPublicationDate(FirstDate).Build()).ToList());
    }

    [Fact]
    public async Task CanDisplayHiddenPlayerAsync_WhoeverAlreadyFoundItKeepsAccess()
    {
        SetupHiddenDay(hiddenDayLeaders: 1, allLeaders: 0, createdPlayers: 0, daysSinceFirstDate: 10);

        var result = await _service.CanDisplayHiddenPlayerAsync(7);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanDisplayHiddenPlayerAsync_RequiresAPerfectRecordOverEveryDay()
    {
        // 4 jours ecoules depuis FirstDate, donc 5 journees a couvrir
        SetupHiddenDay(hiddenDayLeaders: 0, allLeaders: 5, createdPlayers: 0, daysSinceFirstDate: 4);

        var result = await _service.CanDisplayHiddenPlayerAsync(7);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanDisplayHiddenPlayerAsync_ASubmittedPlayerCountsAsACoveredDay()
    {
        SetupHiddenDay(hiddenDayLeaders: 0, allLeaders: 4, createdPlayers: 1, daysSinceFirstDate: 4);

        var result = await _service.CanDisplayHiddenPlayerAsync(7);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanDisplayHiddenPlayerAsync_OneMissingDayIsEnoughToRefuse()
    {
        SetupHiddenDay(hiddenDayLeaders: 0, allLeaders: 4, createdPlayers: 0, daysSinceFirstDate: 4);

        var result = await _service.CanDisplayHiddenPlayerAsync(7);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlayerOfTheDayFromUserPovAsync_BuildsTheCreatorViewFromBothUsers()
    {
        _playerRepository.Setup(_ => _.GetPlayerOfTheDayAsync(FirstDate))
            .ReturnsAsync(PlayerDtoBuilder.Valid().WithId(1).WithName("Zinédine Zidane").WithAllowedNames("zidane").WithCreator(42).Build());
        _userRepository.Setup(_ => _.GetUserByIdAsync(42))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(42).WithLogin("createur").WithUserTypeId((ulong)UserTypes.PowerUser).Build());
        _userRepository.Setup(_ => _.GetUserByIdAsync(7))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(7).WithLogin("joueur").WithUserTypeId((ulong)UserTypes.StandardUser).Build());

        var result = await _service
            .GetPlayerOfTheDayFromUserPovAsync(7, FirstDate);

        result.PlayerId.Should().Be(1);
        result.Login.Should().Be("createur");
        result.Name.Should().BeNull();  // le demandeur n'est ni createur ni admin
    }

    [Fact]
    public async Task GetPlayerOfTheDayFullInfoAsync_DelegatesToTheHandler()
    {
        var full = new PlayerFullDto
        {
            Player = PlayerDtoBuilder.Valid().WithId(1).Build(),
            Clubs = [],
            PlayerClubs = []
        };
        _playerHandler.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(FirstDate)).ReturnsAsync(full);

        var result = await _service.GetPlayerOfTheDayFullInfoAsync(FirstDate);

        result.Should().BeSameAs(full);
    }

    // ------------------------------------------------------------- GetPlayerCluesAsync

    [Fact]
    public async Task GetPlayerCluesAsync_EnglishComesFromThePlayerRow()
    {
        _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
            .ReturnsAsync(PlayerDtoBuilder.Valid().WithId(1).WithClue("the clue").WithEasyClue("the easy clue").Build());

        var clues = await _service.GetPlayerCluesAsync(1, new[] { Languages.en });

        clues.Should().ContainKey(Languages.en);
        clues[Languages.en].clue.Should().Be("the clue");
        clues[Languages.en].easyclue.Should().Be("the easy clue");
        _playerRepository.Verify(
            _ => _.GetClueAsync(It.IsAny<ulong>(), It.IsAny<byte>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task GetPlayerCluesAsync_OtherLanguagesCostTwoLookupsEach()
    {
        _playerRepository.Setup(_ => _.GetClueAsync(1, 0, (ulong)Languages.fr)).ReturnsAsync("indice");
        _playerRepository.Setup(_ => _.GetClueAsync(1, 1, (ulong)Languages.fr)).ReturnsAsync("indice facile");

        var clues = await _service.GetPlayerCluesAsync(1, new[] { Languages.fr });

        clues[Languages.fr].clue.Should().Be("indice");
        clues[Languages.fr].easyclue.Should().Be("indice facile");
        // l'anglais n'etant pas demande, la ligne du joueur n'est jamais lue
        _playerRepository.Verify(_ => _.GetPlayerByIdAsync(It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task GetPlayerCluesAsync_ReturnsEveryRequestedLanguage()
    {
        _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
            .ReturnsAsync(PlayerDtoBuilder.Valid().WithId(1).WithClue("en").WithEasyClue("en easy").Build());
        _playerRepository.Setup(_ => _.GetClueAsync(1, It.IsAny<byte>(), (ulong)Languages.fr))
            .ReturnsAsync("fr");

        var clues = await _service.GetPlayerCluesAsync(1, new[] { Languages.en, Languages.fr });

        clues.Keys.Should().BeEquivalentTo(new[] { Languages.en, Languages.fr });
    }

    [Fact]
    public async Task GetPlayerCluesAsync_WithoutAnyLanguage_ReturnsNothing()
    {
        var clues = await _service.GetPlayerCluesAsync(1, new List<Languages>());

        clues.Should().BeEmpty();
    }

    // ------------------------------------------------------------- UpdatePlayerCluesAsync

    [Fact]
    public async Task UpdatePlayerCluesAsync_WritesTheEnglishRowAndTheTranslations()
    {
        await _service.UpdatePlayerCluesAsync(
            1,
            "new clue",
            "new easy clue",
            new Dictionary<Languages, string?> { { Languages.fr, "  nouvel indice  " } },
            new Dictionary<Languages, string?> { { Languages.fr, "nouvel indice facile" } });

        _playerRepository.Verify(
            _ => _.UpdatePlayerCluesAsync(1, "new clue", "new easy clue"), Times.Once);
        _playerRepository.Verify(
            _ => _.InsertPlayerCluesByLanguageAsync(1, 0,
                It.Is<IReadOnlyDictionary<ulong, string>>(d => d[(ulong)Languages.fr] == "nouvel indice")),
            Times.Once);
        _playerRepository.Verify(
            _ => _.InsertPlayerCluesByLanguageAsync(1, 1, It.IsAny<IReadOnlyDictionary<ulong, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePlayerCluesAsync_WithoutTranslations_OnlyTouchesThePlayerRow()
    {
        await _service.UpdatePlayerCluesAsync(1, "clue", "easy clue", null, null);

        _playerRepository.Verify(_ => _.UpdatePlayerCluesAsync(1, "clue", "easy clue"), Times.Once);
        _playerRepository.Verify(
            _ => _.InsertPlayerCluesByLanguageAsync(
                It.IsAny<ulong>(), It.IsAny<byte>(), It.IsAny<IReadOnlyDictionary<ulong, string>>()),
            Times.Never);
    }

    // ------------------------------------------------------------- GetPlayerSubmissionsAsync

    private void SetupPendingSubmissions(params (ulong playerId, ulong creatorId)[] submissions)
    {
        var dtos = submissions
            .Select(s => PlayerDtoBuilder.Valid().WithId(s.playerId).WithName("Joueur" + s.playerId).WithAllowedNames("joueur" + s.playerId).WithCreator(s.creatorId).WithCountryId((ulong)Countries.FRA).WithPositionId((ulong)Positions.Midfielder).Build())
            .ToList();

        _playerRepository.Setup(_ => _.GetPendingValidationPlayersAsync()).ReturnsAsync(dtos);

        foreach (var creatorId in submissions.Select(s => s.creatorId).Distinct())
        {
            _userRepository.Setup(_ => _.GetUserByIdAsync(creatorId))
                .ReturnsAsync(UserDtoBuilder.Valid().WithId(creatorId).WithLogin("createur" + creatorId).Build());
        }

        _playerHandler
            .Setup(_ => _.GetPlayerFullInfoAsync(It.IsAny<PlayerDto>()))
            .ReturnsAsync<PlayerDto, IPlayerHandler, PlayerFullDto>(p => new PlayerFullDto
            {
                Player = p,
                Clubs = [],
                PlayerClubs = []
            });
    }

    [Fact]
    public async Task GetPlayerSubmissionsAsync_WhenNothingIsPending_ReturnsEmpty()
    {
        SetupPendingSubmissions();

        (await _service.GetPlayerSubmissionsAsync(TestCountryContinents.Map)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlayerSubmissionsAsync_BuildsOnePlayerPerPendingSubmission()
    {
        SetupPendingSubmissions((1, 42), (2, 43));

        var result = await _service.GetPlayerSubmissionsAsync(TestCountryContinents.Map);

        result.Should().HaveCount(2);
        result.Select(p => p.Id).Should().BeEquivalentTo(new ulong[] { 1, 2 });
        result.Select(p => p.Login).Should().BeEquivalentTo(new[] { "createur42", "createur43" });
    }

    [Fact]
    public async Task GetPlayerSubmissionsAsync_ACreatorWithSeveralSubmissionsIsFetchedOnce()
    {
        // le service dedoublonne les createurs avant d'interroger le depot ; il reste
        // en revanche une requete par joueur pour la carriere (cf. N+1 dans le TODO)
        SetupPendingSubmissions((1, 42), (2, 42), (3, 42));

        var result = await _service.GetPlayerSubmissionsAsync(TestCountryContinents.Map);

        result.Should().HaveCount(3);
        _userRepository.Verify(_ => _.GetUserByIdAsync(42), Times.Once);
    }
}
