using System;
using System.Threading.Tasks;
using KikoleSite.Handlers;
using KikoleSite.Repositories;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Services;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services
{
    /// <summary>
    /// Couvre le calcul cumulatif du score d'une journee : c'est la seule partie de
    /// ProposalService qui ne depend d'aucun depot.
    /// </summary>
    public class ProposalServiceTests
    {
        private const ulong RealMadridId = 10;

        private readonly IStringLocalizer<Translations> _localizer;

        public ProposalServiceTests()
        {
            var mock = new Mock<IStringLocalizer<Translations>>();
            mock.Setup(_ => _[It.IsAny<string>()])
                .Returns<string>(key => new LocalizedString(key, key));
            _localizer = mock.Object;
        }

        private static PlayerFullDto Zidane()
        {
            return new PlayerFullDto
            {
                Player = new PlayerDto
                {
                    Id = 1,
                    Name = "Zinédine Zidane",
                    AllowedNames = "zidane;zinedine zidane",
                    YearOfBirth = 1972,
                    CountryId = (ulong)Countries.FR,
                    ContinentId = (ulong)Continents.Europe,
                    PositionId = (ulong)Positions.Midfielder
                },
                Clubs = new List<ClubDto>
                {
                    new ClubDto { Id = RealMadridId, Name = "Real Madrid", AllowedNames = "real;real madrid" }
                },
                PlayerClubs = new List<PlayerClubDto>
                {
                    new PlayerClubDto { PlayerId = 1, ClubId = RealMadridId, HistoryPosition = 4 }
                }
            };
        }

        private static ProposalDto Proposal(ProposalTypes type, string value, bool successful, int minutesOffset)
        {
            return new ProposalDto
            {
                ProposalTypeId = (ulong)type,
                Value = value,
                Successful = (byte)(successful ? 1 : 0),
                ProposalDate = new DateTime(2026, 9, 2),
                CreationDate = new DateTime(2026, 9, 2, 18, 0, 0).AddMinutes(minutesOffset)
            };
        }

        private List<ProposalResponse> Compute(IEnumerable<ProposalDto> proposals, out int points)
        {
            return ProposalService.GetProposalResponsesWithPoints(
                proposals, Zidane(), out points, _localizer);
        }

        [Fact]
        public void WithoutAnyProposal_TheScoreIsTheStartingScore()
        {
            var result = Compute(new List<ProposalDto>(), out var points);

            result.Should().BeEmpty();
            points.Should().Be(ProposalChart.BasePoints);
        }

        [Fact]
        public void ASingleCorrectGuess_CostsNothing()
        {
            Compute(new[] { Proposal(ProposalTypes.Name, "Zidane", true, 5) }, out var points);

            points.Should().Be(1000);
        }

        [Fact]
        public void WrongGuessesAccumulateTheirPenalties()
        {
            Compute(new[]
            {
                Proposal(ProposalTypes.Country, "BR", false, 1),   // -25
                Proposal(ProposalTypes.Position, "1", false, 2),   // -75
                Proposal(ProposalTypes.Club, "Barcelone", false, 3) // -50
            }, out var points);

            points.Should().Be(1000 - 25 - 75 - 50);
        }

        [Fact]
        public void TheClueIsChargedOnTheRunningScoreNotTheStartingOne()
        {
            // -400 puis -50% : 600 puis 300, et non 600 puis 100
            Compute(new[]
            {
                Proposal(ProposalTypes.Name, "Ronaldo", false, 1),
                Proposal(ProposalTypes.Clue, "", true, 2)
            }, out var points);

            points.Should().Be(300);
        }

        [Fact]
        public void ProposalsAreAppliedInChronologicalOrderNotInInputOrder()
        {
            // l'ordre change le resultat des que l'indice est implique, le service
            // doit donc trier par date de creation
            var chronological = new[]
            {
                Proposal(ProposalTypes.Clue, "", true, 1),          // 1000 -> 500
                Proposal(ProposalTypes.Name, "Ronaldo", false, 2)   // 500 -> 100
            };

            Compute(chronological, out var expected);
            Compute(chronological.Reverse().ToArray(), out var shuffled);

            expected.Should().Be(100);
            shuffled.Should().Be(expected);
        }

        [Fact]
        public void TheScoreNeverGoesBelowZero()
        {
            Compute(new[]
            {
                Proposal(ProposalTypes.Name, "Ronaldo", false, 1),
                Proposal(ProposalTypes.Name, "Pele", false, 2),
                Proposal(ProposalTypes.Name, "Maradona", false, 3)
            }, out var points);

            points.Should().Be(0);
        }

        [Fact]
        public void EachResponseCarriesTheRunningTotalAtThatPoint()
        {
            var result = Compute(new[]
            {
                Proposal(ProposalTypes.Country, "BR", false, 1),
                Proposal(ProposalTypes.Position, "1", false, 2)
            }, out _);

            result.Select(r => r.TotalPoints).Should().ContainInOrder(975, 900);
        }

        [Fact]
        public void ThePurchasedTypesAreChargedEvenThoughTheyAreMarkedSuccessful()
        {
            Compute(new[] { Proposal(ProposalTypes.Leaderboard, "", true, 1) }, out var points);

            points.Should().Be(975);
        }
    }

    /// <summary>
    /// Hierarchie d'acces a une journee : Admin > Creator > Found > PaidBoard > None.
    /// C'est ce qui decide si le classement du jour est visible, donc du controle
    /// d'acces fonctionnel et non du simple confort d'affichage.
    /// </summary>
    public class ProposalServiceGrantTests
    {
        private static readonly DateTime Day = ProposalChart.FirstDate;
        private const ulong UserId = 7;

        private readonly Mock<IProposalRepository> _proposalRepository = new Mock<IProposalRepository>();
        private readonly Mock<ILeaderRepository> _leaderRepository = new Mock<ILeaderRepository>();
        private readonly Mock<IUserRepository> _userRepository = new Mock<IUserRepository>();
        private readonly Mock<IPlayerHandler> _playerHandler = new Mock<IPlayerHandler>();
        private readonly ProposalService _service;

        public ProposalServiceGrantTests()
        {
            var localizer = new Mock<IStringLocalizer<Translations>>();
            localizer.Setup(_ => _[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

            _leaderRepository
                .Setup(_ => _.GetUserLeadersAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<ulong>()))
                .ReturnsAsync(new List<LeaderDto>());
            _proposalRepository
                .Setup(_ => _.GetProposalsAsync(It.IsAny<DateTime>(), It.IsAny<ulong>()))
                .ReturnsAsync(new List<ProposalDto>());
            _playerHandler
                .Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new PlayerFullDto
                {
                    Player = new PlayerDto { Id = 1, CreationUserId = 99 },
                    Clubs = new List<ClubDto>(),
                    PlayerClubs = new List<PlayerClubDto>()
                });

            var clock = new Mock<IClock>();
            clock.Setup(_ => _.Today).Returns(Day);

            _service = new ProposalService(
                _proposalRepository.Object,
                _leaderRepository.Object,
                _userRepository.Object,
                _playerHandler.Object,
                localizer.Object,
                clock.Object);
        }

        private void SetupUser(UserTypes type)
        {
            _userRepository.Setup(_ => _.GetUserByIdAsync(UserId))
                .ReturnsAsync(new UserDto { Id = UserId, Login = "joueur", UserTypeId = (ulong)type });
        }

        [Fact]
        public async Task AnAnonymousVisitorGetsNothing()
        {
            var grant = await _service.GetGrantAccessForDayAsync(0, Day).ConfigureAwait(false);

            grant.Should().Be(DayGrantTypes.None);
        }

        [Fact]
        public async Task AnUnknownUserGetsNothing()
        {
            _userRepository.Setup(_ => _.GetUserByIdAsync(UserId)).ReturnsAsync((UserDto)null);

            var grant = await _service.GetGrantAccessForDayAsync(UserId, Day).ConfigureAwait(false);

            grant.Should().Be(DayGrantTypes.None);
        }

        [Fact]
        public async Task AnAdministratorIsGrantedWithoutAnyLookup()
        {
            SetupUser(UserTypes.Administrator);

            var grant = await _service.GetGrantAccessForDayAsync(UserId, Day).ConfigureAwait(false);

            grant.Should().Be(DayGrantTypes.Admin);
            _playerHandler.Verify(
                _ => _.GetPlayerOfTheDayFullInfoAsync(It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task TheCreatorOfTheDayOutranksHavingFoundIt()
        {
            SetupUser(UserTypes.StandardUser);
            _playerHandler
                .Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Day))
                .ReturnsAsync(new PlayerFullDto
                {
                    Player = new PlayerDto { Id = 1, CreationUserId = UserId },
                    Clubs = new List<ClubDto>(),
                    PlayerClubs = new List<PlayerClubDto>()
                });

            var grant = await _service.GetGrantAccessForDayAsync(UserId, Day).ConfigureAwait(false);

            grant.Should().Be(DayGrantTypes.Creator);
        }

        [Fact]
        public async Task HavingALeaderRowGrantsFound()
        {
            SetupUser(UserTypes.StandardUser);
            _leaderRepository
                .Setup(_ => _.GetUserLeadersAsync(Day, Day, true, UserId))
                .ReturnsAsync(new List<LeaderDto> { new LeaderDto { UserId = UserId } });

            var grant = await _service.GetGrantAccessForDayAsync(UserId, Day).ConfigureAwait(false);

            grant.Should().Be(DayGrantTypes.Found);
        }

        [Fact]
        public async Task ASuccessfulNameProposalAlsoGrantsFound()
        {
            // filet pour le cas ou la ligne "leaders" manque : la proposition gagnante suffit
            SetupUser(UserTypes.StandardUser);
            _proposalRepository
                .Setup(_ => _.GetProposalsAsync(Day, UserId))
                .ReturnsAsync(new List<ProposalDto>
                {
                    new ProposalDto { ProposalTypeId = (ulong)ProposalTypes.Name, Successful = 1 }
                });

            var grant = await _service.GetGrantAccessForDayAsync(UserId, Day).ConfigureAwait(false);

            grant.Should().Be(DayGrantTypes.Found);
        }

        [Fact]
        public async Task BuyingTheLeaderboardGrantsPaidBoardOnly()
        {
            SetupUser(UserTypes.StandardUser);
            _proposalRepository
                .Setup(_ => _.GetProposalsAsync(Day, UserId))
                .ReturnsAsync(new List<ProposalDto>
                {
                    new ProposalDto { ProposalTypeId = (ulong)ProposalTypes.Leaderboard, Successful = 1 }
                });

            var grant = await _service.GetGrantAccessForDayAsync(UserId, Day).ConfigureAwait(false);

            grant.Should().Be(DayGrantTypes.PaidBoard);
        }

        [Fact]
        public async Task SearchingWithoutFindingOrBuyingGrantsNothing()
        {
            SetupUser(UserTypes.StandardUser);
            _proposalRepository
                .Setup(_ => _.GetProposalsAsync(Day, UserId))
                .ReturnsAsync(new List<ProposalDto>
                {
                    new ProposalDto { ProposalTypeId = (ulong)ProposalTypes.Club, Successful = 0 },
                    new ProposalDto { ProposalTypeId = (ulong)ProposalTypes.Name, Successful = 0 }
                });

            var grant = await _service.GetGrantAccessForDayAsync(UserId, Day).ConfigureAwait(false);

            grant.Should().Be(DayGrantTypes.None);
        }

        [Fact]
        public async Task AskingForAClueIsNotEnoughToSeeTheBoard()
        {
            // seul l'achat du classement ouvre l'acces, pas n'importe quel achat
            SetupUser(UserTypes.StandardUser);
            _proposalRepository
                .Setup(_ => _.GetProposalsAsync(Day, UserId))
                .ReturnsAsync(new List<ProposalDto>
                {
                    new ProposalDto { ProposalTypeId = (ulong)ProposalTypes.Clue, Successful = 1 }
                });

            var grant = await _service.GetGrantAccessForDayAsync(UserId, Day).ConfigureAwait(false);

            grant.Should().Be(DayGrantTypes.None);
        }
    }
}
