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
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services
{
    public class PlayerServiceTests
    {
        // les dates sont exprimees relativement a FirstDate pour que les tests
        // survivent au changement de cette constante
        private static readonly DateTime FirstDate = ProposalChart.FirstDate;

        private readonly Mock<IPlayerHandler> _playerHandler = new Mock<IPlayerHandler>();
        private readonly Mock<IPlayerRepository> _playerRepository = new Mock<IPlayerRepository>();
        private readonly Mock<IUserRepository> _userRepository = new Mock<IUserRepository>();
        private readonly Mock<ILeaderRepository> _leaderRepository = new Mock<ILeaderRepository>();
        private readonly Mock<IClock> _clock = new Mock<IClock>();
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
                new Random(1));
        }

        private static PlayerRequest Request()
        {
            return new PlayerRequest
            {
                Name = "Zinédine Zidane",
                YearOfBirth = 1972,
                Country = Countries.FR,
                Continent = Continents.Europe,
                Position = Positions.Midfielder,
                AllowedNames = new List<string> { "Zidane" },
                Clubs = new List<PlayerClubRequest>
                {
                    new PlayerClubRequest { ClubId = 1, HistoryPosition = 1 },
                    new PlayerClubRequest { ClubId = 2, HistoryPosition = 2 }
                },
                ClueEn = "clue",
                EasyClueEn = "easy clue"
            };
        }

        // ------------------------------------------------------------- CreatePlayerAsync

        [Fact]
        public async Task CreatePlayerAsync_WhenAskedForTheNextSlot_TakesTheDayAfterTheLatestOne()
        {
            var request = Request();
            request.SetLatestProposalDate = true;
            _playerRepository.Setup(_ => _.GetLatestProposalDateAsync())
                .ReturnsAsync(FirstDate.AddDays(4));
            _playerRepository.Setup(_ => _.CreatePlayerAsync(It.IsAny<PlayerDto>())).ReturnsAsync(9UL);

            await _service.CreatePlayerAsync(request, 42);

            _playerRepository.Verify(
                _ => _.CreatePlayerAsync(It.Is<PlayerDto>(d => d.ProposalDate == FirstDate.AddDays(5))),
                Times.Once);
        }

        [Fact]
        public async Task CreatePlayerAsync_AnExplicitDateWins()
        {
            var request = Request();
            request.SetLatestProposalDate = true;
            request.ProposalDate = FirstDate.AddDays(10);
            _playerRepository.Setup(_ => _.CreatePlayerAsync(It.IsAny<PlayerDto>())).ReturnsAsync(9UL);

            await _service.CreatePlayerAsync(request, 42);

            _playerRepository.Verify(_ => _.GetLatestProposalDateAsync(), Times.Never);
            _playerRepository.Verify(
                _ => _.CreatePlayerAsync(It.Is<PlayerDto>(d => d.ProposalDate == FirstDate.AddDays(10))),
                Times.Once);
        }

        [Fact]
        public async Task CreatePlayerAsync_WithoutAnyDate_StaysPendingValidation()
        {
            var request = Request();
            _playerRepository.Setup(_ => _.CreatePlayerAsync(It.IsAny<PlayerDto>())).ReturnsAsync(9UL);

            await _service.CreatePlayerAsync(request, 42);

            _playerRepository.Verify(
                _ => _.CreatePlayerAsync(It.Is<PlayerDto>(d => d.ProposalDate == null)),
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
            var request = Request();
            request.ClueLanguages = new Dictionary<Languages, string>
            {
                { Languages.fr, "  indice francais  " },
                { Languages.en, "   " }
            };
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
                .ReturnsAsync(new PlayerDto { Id = 1, Clue = "the clue", EasyClue = "the easy clue" });

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
                .ReturnsAsync(new PlayerDto { Id = 1, Clue = "the clue", EasyClue = "the easy clue" });
            _playerRepository.Setup(_ => _.GetClueAsync(1, expectedFlag, (ulong)Languages.fr))
                .ReturnsAsync("indice traduit");

            var result = await _service
                .GetPlayerClueAsync(FirstDate, isEasy, Languages.fr);

            result.Should().Be("indice traduit");
        }

        // ------------------------------------------------------------- validation d'une soumission

        [Fact]
        public async Task ValidatePlayerSubmissionAsync_WhenThePlayerDoesNotExist_ReportsNotFound()
        {
            _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1)).ReturnsAsync((PlayerDto)null);

            var (error, userId, badges) = await _service
                .ValidatePlayerSubmissionAsync(new PlayerSubmissionValidationRequest { PlayerId = 1 });

            error.Should().Be(PlayerSubmissionErrors.PlayerNotFound);
            userId.Should().Be(0);
            badges.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidatePlayerSubmissionAsync_WhenAlreadyScheduled_IsRefused()
        {
            _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
                .ReturnsAsync(new PlayerDto { Id = 1, ProposalDate = FirstDate });

            var (error, _, _) = await _service
                .ValidatePlayerSubmissionAsync(new PlayerSubmissionValidationRequest { PlayerId = 1 });

            error.Should().Be(PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused);
        }

        [Fact]
        public async Task ValidatePlayerSubmissionAsync_WhenAlreadyRefused_IsRefusedAgain()
        {
            _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
                .ReturnsAsync(new PlayerDto { Id = 1, RejectDate = FirstDate });

            var (error, _, _) = await _service
                .ValidatePlayerSubmissionAsync(new PlayerSubmissionValidationRequest { PlayerId = 1 });

            error.Should().Be(PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused);
        }

        private void SetupPendingPlayer(int acceptedPlayersOfCreator)
        {
            _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
                .ReturnsAsync(new PlayerDto
                {
                    Id = 1,
                    CreationUserId = 42,
                    Clue = "current clue",
                    EasyClue = "current easy clue"
                });
            _playerRepository.Setup(_ => _.GetLatestProposalDateAsync()).ReturnsAsync(FirstDate.AddDays(2));
            _playerRepository.Setup(_ => _.GetPlayersByCreatorAsync(42, true))
                .ReturnsAsync(Enumerable.Range(0, acceptedPlayersOfCreator)
                    .Select(_ => new PlayerDto()).ToList());
        }

        private static PlayerSubmissionValidationRequest Acceptance()
        {
            return new PlayerSubmissionValidationRequest
            {
                PlayerId = 1,
                IsAccepted = true,
                ClueEditLanguages = new Dictionary<Languages, string> { { Languages.fr, "indice" } },
                EasyClueEditLanguages = new Dictionary<Languages, string> { { Languages.fr, "facile" } }
            };
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
            var request = Acceptance();
            request.ClueEditEn = "  nouvel indice  ";

            await _service.ValidatePlayerSubmissionAsync(request);

            _playerRepository.Verify(
                _ => _.UpdatePlayerCluesAsync(1, "nouvel indice", "current easy clue"), Times.Once);
        }

        [Fact]
        public async Task ValidatePlayerSubmissionAsync_WhenRefused_NoDateIsAssignedAndNoBadgeIsGranted()
        {
            _playerRepository.Setup(_ => _.GetPlayerByIdAsync(1))
                .ReturnsAsync(new PlayerDto { Id = 1, CreationUserId = 42 });

            var request = new PlayerSubmissionValidationRequest
            {
                PlayerId = 1,
                IsAccepted = false,
                RefusalReason = "doublon"
            };

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
                _ => _.ChangePlayerProposalDateAsync(It.IsAny<ulong>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task ReassignPlayersOfTheDayAsync_SpreadsFuturePlayersOverConsecutiveDays()
        {
            _clock.Setup(_ => _.IsTomorrowIn(30)).Returns(false);
            _playerRepository.Setup(_ => _.GetPlayersOfTheDayAsync(FirstDate.AddDays(1), null))
                .ReturnsAsync(new List<PlayerDto>
                {
                    new PlayerDto { Id = 1 }, new PlayerDto { Id = 2 }, new PlayerDto { Id = 3 }
                });

            var assigned = new List<DateTime>();
            _playerRepository
                .Setup(_ => _.ChangePlayerProposalDateAsync(It.IsAny<ulong>(), It.IsAny<DateTime>()))
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
                .Setup(_ => _.GetUserLeadersAsync(ProposalChart.HiddenDate, ProposalChart.HiddenDate, false, 7))
                .ReturnsAsync(Enumerable.Range(0, hiddenDayLeaders).Select(_ => new LeaderDto()).ToList());
            _leaderRepository
                .Setup(_ => _.GetUserLeadersAsync(ProposalChart.FirstDate, null, false, 7))
                .ReturnsAsync(Enumerable.Range(0, allLeaders).Select(_ => new LeaderDto()).ToList());
            _playerRepository
                .Setup(_ => _.GetPlayersByCreatorAsync(7, true))
                .ReturnsAsync(Enumerable.Range(0, createdPlayers)
                    .Select(_ => new PlayerDto { ProposalDate = FirstDate }).ToList());
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
                .ReturnsAsync(new PlayerDto
                {
                    Id = 1, Name = "Zinédine Zidane", AllowedNames = "zidane", CreationUserId = 42
                });
            _userRepository.Setup(_ => _.GetUserByIdAsync(42))
                .ReturnsAsync(new UserDto { Id = 42, Login = "createur", UserTypeId = (ulong)UserTypes.PowerUser });
            _userRepository.Setup(_ => _.GetUserByIdAsync(7))
                .ReturnsAsync(new UserDto { Id = 7, Login = "joueur", UserTypeId = (ulong)UserTypes.StandardUser });

            var result = await _service
                .GetPlayerOfTheDayFromUserPovAsync(7, FirstDate);

            result.PlayerId.Should().Be(1);
            result.Login.Should().Be("createur");
            result.Name.Should().BeNull();  // le demandeur n'est ni createur ni admin
        }
    }
}
