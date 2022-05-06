using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Helpers;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Interfaces.Services;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Services
{
    /// <summary>
    /// Leader service implementation.
    /// </summary>
    /// <seealso cref="ILeaderService"/>
    public class LeaderService : ILeaderService
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IChallengeRepository _challengeRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly IClock _clock;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
        /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
        /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
        /// <param name="challengeRepository">Instance of <see cref="IChallengeRepository"/>.</param>
        /// <param name="proposalRepository">Instance of <see cref="IProposalRepository"/>.</param>
        /// <param name="clock">Clock service.</param>
        public LeaderService(IPlayerRepository playerRepository,
            ILeaderRepository leaderRepository,
            IUserRepository userRepository,
            IChallengeRepository challengeRepository,
            IProposalRepository proposalRepository,
            IClock clock)
        {
            _playerRepository = playerRepository;
            _leaderRepository = leaderRepository;
            _userRepository = userRepository;
            _challengeRepository = challengeRepository;
            _proposalRepository = proposalRepository;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Leader>> GetLeadersOfTheDayAsync(DateTime day, DayLeaderSorts sort)
        {
            var pDay = await _playerRepository
                .GetPlayerOfTheDayAsync(day)
                .ConfigureAwait(false);

            var leadersDto = await _leaderRepository
                .GetLeadersAtDateAsync(day, sort == DayLeaderSorts.TotalPoints)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var leaders = leadersDto
                .Select(dto => new Leader(dto, users))
                .ToList();

            var userCreator = users.SingleOrDefault(u => u.Id == pDay.CreationUserId);
            if (userCreator != null && (pDay.HideCreator == 0 || day.Date < _clock.Today))
                leaders.Add(new Leader(userCreator, day, leadersDto));

            return sort == DayLeaderSorts.BestTime
                ? leaders.SetPositions(l => l.BestTime.TotalMinutes, l => l.TotalPoints, false, true, (l, i) => l.Position = i)
                : leaders.SetPositions(l => l.TotalPoints, l => l.BestTime.TotalMinutes, true, false, (l, i) => l.Position = i);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Leader>> GetPvpLeadersAsync(DateTime? minimalDate, DateTime? maximalDate)
        {
            return await GetLeadersAsync(
                    minimalDate, maximalDate, LeaderSorts.TotalPoints, true)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Leader>> GetPveLeadersAsync(DateTime? minimalDate, DateTime? maximalDate, LeaderSorts leaderSort)
        {
            return await GetLeadersAsync(
                    minimalDate, maximalDate, leaderSort, false)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Awards> GetAwardsAsync(int year, int month)
        {
            var awards = new Awards
            {
                Year = year,
                Month = month
            };

            var firstDayOfMonth = new DateTime(awards.Year, awards.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var leaderDtos = await _leaderRepository
                .GetLeadersAsync(firstDayOfMonth, lastDayOfMonth, true)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var players = await _playerRepository
                .GetPlayersOfTheDayAsync(firstDayOfMonth, lastDayOfMonth)
                .ConfigureAwait(false);

            var kikoles = await _leaderRepository
                .GetKikoleAwardsAsync(firstDayOfMonth, lastDayOfMonth)
                .ConfigureAwait(false);

            var leaders = ComputePveLeaders(leaderDtos, users, players);

            awards.PointsAwards = ComputeTopThreeLeadersAwards<PointsAward, int>(
                leaders,
                (a, l) => a.Points = l.TotalPoints,
                _ => _.Points,
                true);
            awards.TimeAwards = ComputeTopThreeLeadersAwards<TimeAward, TimeSpan>(
                leaders,
                (a, l) =>
                {
                    a.Time = l.BestTime;
                    a.PlayerName = players.Single(p =>
                        l.BestTimeDate.Date == p.ProposalDate.Value.Date).Name;
                },
                _ => _.Time,
                false);
            awards.CountAwards = ComputeTopThreeLeadersAwards<CountAward, int>(
                leaders,
                (a, l) => a.Count = l.SuccessCount,
                _ => _.Count,
                true);
            awards.HardestKikoles = GetTopThreeKikoles(kikoles, false);
            awards.EasiestKikoles = GetTopThreeKikoles(kikoles, true);

            return awards;
        }

        /// <inheritdoc />
        public async Task<UserStat> GetUserStatisticsAsync(ulong userId)
        {
            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return null;

            var stats = new List<DailyUserStat>();

            var currentDate = await _playerRepository
                .GetFirstDateAsync()
                .ConfigureAwait(false);
            currentDate = currentDate.Date;

            var now = _clock.Today;
            while (currentDate <= now)
            {
                var pDay = await _playerRepository
                    .GetPlayerOfTheDayAsync(currentDate)
                    .ConfigureAwait(false);

                var proposals = await _proposalRepository
                    .GetProposalsAsync(currentDate, userId)
                    .ConfigureAwait(false);

                var leaders = await _leaderRepository
                    .GetLeadersAtDateAsync(currentDate, false)
                    .ConfigureAwait(false);

                var meLeader = leaders.SingleOrDefault(l => l.UserId == userId);

                var isCreator = (pDay.ProposalDate.Value.Date < now || pDay.HideCreator == 0)
                    && userId == pDay.CreationUserId;

                var singleStat = isCreator
                    ? new DailyUserStat(currentDate, pDay.Name, leaders)
                    : new DailyUserStat(userId, currentDate, pDay.Name, proposals.Any(_ => _.IsCurrentDay), proposals.Count > 0, leaders, meLeader);

                stats.Add(singleStat);
                currentDate = currentDate.AddDays(1);
            }

            return new UserStat(stats, user.Login);
        }

        /// <inheritdoc />
        public async Task ComputeMissingLeadersAsync()
        {
            var players = await _playerRepository
                .GetPlayersOfTheDayAsync(null, _clock.Today)
                .ConfigureAwait(false);

            foreach (var playerOfTheDay in players)
            {
                var usersId = await _proposalRepository
                    .GetMissingUsersAsLeaderAsync(playerOfTheDay.ProposalDate.Value)
                    .ConfigureAwait(false);

                foreach (var userId in usersId)
                {
                    var proposals = await _proposalRepository
                        .GetProposalsAsync(playerOfTheDay.ProposalDate.Value, userId)
                        .ConfigureAwait(false);

                    var points = ProposalChart.Default.BasePoints;

                    foreach (var proposal in proposals.OrderBy(p => p.CreationDate))
                    {
                        var minusPoints = ProposalChart.Default.ProposalTypesCost[(ProposalTypes)proposal.ProposalTypeId];
                        if (proposal.Successful == 0)
                            points -= minusPoints;

                        if (proposal.Successful > 0 && (ProposalTypes)proposal.ProposalTypeId == ProposalTypes.Name)
                        {
                            await _leaderRepository
                                .CreateLeaderAsync(new LeaderDto
                                {
                                    Points = (ushort)(points < 0 ? 0 : points),
                                    ProposalDate = playerOfTheDay.ProposalDate.Value,
                                    Time = (proposal.CreationDate - playerOfTheDay.ProposalDate.Value).ToRoundMinutes(),
                                    UserId = userId,
                                    CreationDate = proposal.CreationDate
                                })
                                .ConfigureAwait(false);
                            // we had for a while a bug of proposals after the player has been found
                            break;
                        }
                    }
                }
            }
        }

        private async Task<IReadOnlyCollection<Leader>> GetLeadersAsync(DateTime? minimalDate, DateTime? maximalDate, LeaderSorts leaderSort, bool includePvp)
        {
            var onTimeOnly = leaderSort != LeaderSorts.TotalPointsOverall
                && leaderSort != LeaderSorts.SuccessCountOverall;

            var leaderDtos = await _leaderRepository
                .GetLeadersAsync(minimalDate, maximalDate, onTimeOnly)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            IReadOnlyCollection<Leader> leaders;
            if (includePvp)
            {
                leaderSort = LeaderSorts.TotalPoints;

                var challenges = await _challengeRepository
                    .GetAcceptedChallengesAsync(minimalDate, maximalDate)
                    .ConfigureAwait(false);

                leaders = ComputePvpLeaders(challenges, leaderDtos, users);
            }
            else
            {
                var players = await _playerRepository
                    .GetPlayersOfTheDayAsync(minimalDate, maximalDate.GetValueOrDefault(_clock.Today))
                    .ConfigureAwait(false);

                leaders = ComputePveLeaders(leaderDtos, users, players);
            }

            return SortWithPosition(leaders, leaderSort);
        }

        private static IReadOnlyCollection<T> ComputeTopThreeLeadersAwards<T, TProp>(
            IReadOnlyCollection<Leader> leaders,
            Action<T, Leader> setAwardPropFunc,
            Func<T, TProp> getPosKeyFunc,
            bool descending)
            where T : BaseAward, new()
            where TProp : struct
        {
            return leaders
                .Select(_ =>
                {
                    var awd = new T
                    {
                        Name = _.Login
                    };
                    setAwardPropFunc(awd, _);
                    return awd;
                })
                .SetPositions(_ => getPosKeyFunc(_), descending, (_, x) => _.Position = x)
                .Where(_ => _.Position <= 3)
                .ToList();
        }

        private static List<KikoleAward> GetTopThreeKikoles(
            IReadOnlyCollection<KikoleAwardDto> kikoles, bool descending)
        {
            return kikoles
                .Select(_ => new KikoleAward
                {
                    AveragePoints = (int)Math.Round(_.AvgPts),
                    Name = _.Name
                })
                .SetPositions(_ => _.AveragePoints, descending, (_, x) => _.Position = x)
                .Where(_ => _.Position <= 3)
                .ToList();
        }

        private static IReadOnlyCollection<Leader> SortWithPosition(
            IEnumerable<Leader> leaders, LeaderSorts sort)
        {
            switch (sort)
            {
                case LeaderSorts.SuccessCount:
                case LeaderSorts.SuccessCountOverall:
                    leaders = leaders.SetPositions(l => l.SuccessCount, true, (l, i) => l.Position = i);
                    break;
                case LeaderSorts.BestTime:
                    leaders = leaders.SetPositions(l => l.BestTime, false, (l, i) => l.Position = i);
                    break;
                case LeaderSorts.TotalPoints:
                case LeaderSorts.TotalPointsOverall:
                    leaders = leaders.SetPositions(l => l.TotalPoints, true, (l, i) => l.Position = i);
                    break;
            }

            return leaders.ToList();
        }

        private IReadOnlyCollection<Leader> ComputePvpLeaders(
            IReadOnlyCollection<ChallengeDto> challenges,
            IReadOnlyCollection<LeaderDto> leaderDtos,
            IReadOnlyCollection<UserDto> users)
        {
            var leaders = new List<Leader>();

            foreach (var challenge in challenges)
            {
                var hostLead = leaderDtos.SingleOrDefault(l => l.UserId == challenge.HostUserId && l.ProposalDate == challenge.ChallengeDate);
                var guestLead = leaderDtos.SingleOrDefault(l => l.UserId == challenge.GuestUserId && l.ProposalDate == challenge.ChallengeDate);
                var pointsDelta = Models.Challenge.ComputeHostPoints(challenge, hostLead, guestLead);
                if (pointsDelta != 0)
                {
                    leaders.Add(new Leader(challenge.HostUserId, pointsDelta + (hostLead?.Points ?? 0), users));
                    leaders.Add(new Leader(challenge.GuestUserId, -pointsDelta + (guestLead?.Points ?? 0), users));
                }
            }

            return leaders
                .GroupBy(l => l.UserId)
                .Select(l => new Leader(l))
                .ToList();
        }

        private IReadOnlyCollection<Leader> ComputePveLeaders(
            IReadOnlyCollection<LeaderDto> leaderDtos,
            IReadOnlyCollection<UserDto> users,
            IReadOnlyCollection<PlayerDto> players)
        {
            return leaderDtos
                .GroupBy(leaderDto => leaderDto.UserId)
                .Select(leaderDto => new Leader(leaderDto, users)
                    .WithPointsFromSubmittedPlayers(
                        GetDatesWithPlayerCreation(players, leaderDto), leaderDtos))
                .ToList();
        }

        private IEnumerable<DateTime> GetDatesWithPlayerCreation(IReadOnlyCollection<PlayerDto> players, IGrouping<ulong, LeaderDto> leaderDto)
        {
            return players
                .Where(p => p.CreationUserId == leaderDto.Key
                    && ((p.ProposalDate.Value < _clock.Today) || p.HideCreator == 0))
                .Select(d => d.ProposalDate.Value);
        }
    }
}
