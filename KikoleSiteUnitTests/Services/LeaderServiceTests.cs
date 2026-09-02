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

namespace KikoleSiteUnitTests.Services
{
    public class LeaderServiceTests
    {
        private static readonly DateTime Day = ProposalChart.FirstDate;

        private readonly Mock<IPlayerRepository> _playerRepository = new Mock<IPlayerRepository>();
        private readonly Mock<ILeaderRepository> _leaderRepository = new Mock<ILeaderRepository>();
        private readonly Mock<IUserRepository> _userRepository = new Mock<IUserRepository>();
        private readonly Mock<IProposalRepository> _proposalRepository = new Mock<IProposalRepository>();
        private readonly Mock<IPlayerHandler> _playerHandler = new Mock<IPlayerHandler>();
        private readonly Mock<IClock> _clock = new Mock<IClock>();
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
                localizer.Object,
                _playerHandler.Object);
        }

        private void SetupUsers(params (ulong id, string login)[] users)
        {
            foreach (var (id, login) in users)
            {
                _userRepository.Setup(_ => _.GetUserByIdAsync(id))
                    .ReturnsAsync(new UserDto
                    {
                        Id = id,
                        Login = login,
                        UserTypeId = (ulong)UserTypes.StandardUser
                    });
            }
        }

        private static LeaderDto Leader(ulong userId, ushort points, int minutes, DateTime? date = null)
        {
            return new LeaderDto
            {
                UserId = userId,
                Points = points,
                Time = minutes,
                ProposalDate = date ?? Day,
                CreationDate = (date ?? Day).AddMinutes(minutes)
            };
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
                new[] { new PlayerDto { Id = 9, CreationUserId = 1, ProposalDate = Day.AddDays(2) } });

            var result = await _service
                .GetLeaderboardAsync(Day, Day.AddDays(2), LeaderSorts.TotalPoints);

            var item = result.Single();
            item.Points.Should().Be(800 + 500 + ProposalChart.SubmissionPoints);
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
                new[] { new PlayerDto { Id = 9, CreationUserId = 5, ProposalDate = Day } });

            var result = await _service
                .GetLeaderboardAsync(Day, Day, LeaderSorts.TotalPoints);

            result.Single().Points.Should().Be(ProposalChart.SubmissionPoints);
            result.Single().KikolesFound.Should().Be(0);
        }

        [Fact]
        public async Task GetLeaderboardAsync_SomeoneWhoNeverFoundAnythingGetsTheWorstPossibleTime()
        {
            SetupUsers((5, "createur"));
            SetupLeaderboard(
                new List<LeaderDto>(),
                new[] { new PlayerDto { Id = 9, CreationUserId = 5, ProposalDate = Day } });

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
            _userRepository.Setup(_ => _.GetUserByIdAsync(1))
                .ReturnsAsync(new UserDto { Id = 1, Login = "admin", UserTypeId = (ulong)UserTypes.Administrator });
            SetupLeaderboard(new[] { Leader(1, 900, 60) }, new List<PlayerDto>());

            var result = await _service
                .GetLeaderboardAsync(Day, Day, LeaderSorts.TotalPoints);

            result.Should().BeEmpty();
        }

        // ------------------------------------------------------------- ComputeMissingLeadersAsync

        private static ProposalDto Proposal(ProposalTypes type, bool successful, int minutes)
        {
            return new ProposalDto
            {
                ProposalTypeId = (ulong)type,
                Successful = (byte)(successful ? 1 : 0),
                ProposalDate = Day,
                CreationDate = Day.AddMinutes(minutes)
            };
        }

        private void SetupMissingLeader(params ProposalDto[] proposals)
        {
            var player = new PlayerDto
            {
                Id = 1,
                Name = "Zidane",
                AllowedNames = "zidane",
                ProposalDate = Day,
                YearOfBirth = 1972,
                CountryId = (ulong)Countries.FR,
                ContinentId = (ulong)Continents.Europe,
                PositionId = (ulong)Positions.Midfielder
            };

            _playerRepository
                .Setup(_ => _.GetPlayersOfTheDayAsync(null, Day))
                .ReturnsAsync(new List<PlayerDto> { player });
            _playerHandler
                .Setup(_ => _.GetPlayerFullInfoAsync(It.IsAny<PlayerDto>()))
                .ReturnsAsync(new PlayerFullDto
                {
                    Player = player,
                    Clubs = new List<ClubDto>(),
                    PlayerClubs = new List<PlayerClubDto>()
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
            SetupMissingLeader(new ProposalDto
            {
                ProposalTypeId = (ulong)ProposalTypes.Name,
                Successful = 1,
                ProposalDate = Day,
                CreationDate = Day.AddMinutes(61).AddSeconds(30)
            });

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
                Player = new PlayerDto
                {
                    Id = 1,
                    Name = "Zidane",
                    AllowedNames = "zidane",
                    YearOfBirth = 1972,
                    CountryId = (ulong)Countries.FR,
                    ContinentId = (ulong)Continents.Europe,
                    PositionId = (ulong)Positions.Midfielder
                },
                Clubs = new List<ClubDto>(),
                PlayerClubs = new List<PlayerClubDto>()
            };

            var localizer = new Mock<IStringLocalizer<Translations>>();
            localizer.Setup(_ => _[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

            ProposalService.GetProposalResponsesWithPoints(
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
                    Player = new PlayerDto
                    {
                        Id = 1,
                        Name = "Zidane",
                        AllowedNames = "zidane",
                        CreationUserId = creatorId
                    },
                    Clubs = new List<ClubDto>(),
                    PlayerClubs = new List<PlayerClubDto>()
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
            creator.Points.Should().Be(ProposalChart.SubmissionPoints);
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
                    new ProposalDto
                    {
                        UserId = 2,
                        ProposalTypeId = (ulong)ProposalTypes.Club,
                        Value = "Barcelone",
                        Successful = 0,
                        ProposalDate = Day,
                        CreationDate = Day.AddMinutes(20)
                    }
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
                    new ProposalDto
                    {
                        UserId = 1,
                        ProposalTypeId = (ulong)ProposalTypes.Name,
                        Value = "Zidane",
                        Successful = 1,
                        ProposalDate = Day,
                        CreationDate = Day.AddMinutes(60)
                    }
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
}
