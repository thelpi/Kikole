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
        private readonly IProposalRepository _proposalRepository;
        private readonly IClock _clock;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
        /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
        /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
        /// <param name="proposalRepository">Instance of <see cref="IProposalRepository"/>.</param>
        /// <param name="clock">Clock service.</param>
        public LeaderService(IPlayerRepository playerRepository,
            ILeaderRepository leaderRepository,
            IUserRepository userRepository,
            IProposalRepository proposalRepository,
            IClock clock)
        {
            _playerRepository = playerRepository;
            _leaderRepository = leaderRepository;
            _userRepository = userRepository;
            _proposalRepository = proposalRepository;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<LeaderboardItem>> GetLeaderboardAsync(DateTime startDate, DateTime endDate, LeaderSorts leaderSort)
        {
            if (startDate.Date > endDate.Date)
            {
                var tmp = endDate;
                endDate = startDate;
                startDate = tmp;
            }

            var onTimeOnly = leaderSort != LeaderSorts.SuccessCountOverall
                && leaderSort != LeaderSorts.TotalPointsOverall;

            var leaders = await _leaderRepository
                .GetLeadersAsync(startDate, endDate, onTimeOnly)
                .ConfigureAwait(false);

            // we need players to get creators
            var players = await _playerRepository
                .GetPlayersOfTheDayAsync(startDate, endDate)
                .ConfigureAwait(false);

            // mix of leaders and creators (some might not play during the period)
            var allUsersId = players
                .Select(_ => _.CreationUserId)
                .Concat(leaders.Select(_ => _.UserId))
                .Distinct();

            var users = await GetUsersFromIdsAsync(allUsersId).ConfigureAwait(false);

            var items = new List<LeaderboardItem>();
            foreach (var user in users)
            {
                var userLeaders = leaders.Where(_ => _.UserId == user.Id);
                var userPlayers = players.Where(_ => _.CreationUserId == user.Id);

                var points = userLeaders.Sum(_ => _.Points);
                foreach (var userPlayer in userPlayers)
                    points += GetSubmittedPlayerPoints(leaders, userPlayer.ProposalDate.Value);

                items.Add(new LeaderboardItem
                {
                    BestTime = userLeaders.Any()
                        ? userLeaders.Select(_ => new TimeSpan(0, _.Time, 0)).Min()
                        : new TimeSpan(23, 59, 59),
                    KikolesAttempted = await _proposalRepository
                        .GetDaysCountWithProposalAsync(startDate, endDate, user.Id, onTimeOnly)
                        .ConfigureAwait(false),
                    KikolesFound = userLeaders.Count(),
                    KikolesProposed = userPlayers.Count(),
                    Points = points,
                    UserId = user.Id,
                    UserName = user.Login
                });
            }

            switch (leaderSort)
            {
                case LeaderSorts.BestTime:
                    items = items.SetPositions(_ => _.BestTime, false, (_, r) => _.Rank = r);
                    break;
                case LeaderSorts.SuccessCountOverall:
                case LeaderSorts.SuccessCount:
                    items = items.SetPositions(_ => _.KikolesFound, true, (_, r) => _.Rank = r);
                    break;
                case LeaderSorts.TotalPointsOverall:
                case LeaderSorts.TotalPoints:
                    items = items.SetPositions(_ => _.Points, true, (_, r) => _.Rank = r);
                    break;
            }

            return items;
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
                    ? new DailyUserStat(currentDate, pDay.Name, GetSubmittedPlayerPoints(leaders, currentDate))
                    : new DailyUserStat(userId, currentDate, pDay.Name, proposals.Any(_ => _.IsCurrentDay), proposals.Count > 0, leaders, meLeader);

                stats.Add(singleStat);
                currentDate = currentDate.AddDays(1);
            }

            return new UserStat(stats, user.Login, user.CreationDate);
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
                        var (minusPoints, isRate) = ProposalChart.Default.ProposalTypesCost[(ProposalTypes)proposal.ProposalTypeId];
                        if (proposal.Successful == 0)
                        {
                            if (isRate)
                                minusPoints = (int)Math.Round(points * minusPoints / (decimal)100);
                            points -= minusPoints;
                        }

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

        /// <inheritdoc />
        public async Task<Dayboard> GetDayboardAsync(DateTime day, DayLeaderSorts sort)
        {
            day = day.Date;

            var leaders = await _leaderRepository
                .GetLeadersAtDateAsync(day, false)
                .ConfigureAwait(false);

            var proposals = await _proposalRepository
                .GetProposalsAsync(day, false)
                .ConfigureAwait(false);

            var player = await _playerRepository
                .GetPlayerOfTheDayAsync(day)
                .ConfigureAwait(false);

            var leaderUsers = leaders.Select(_ => _.UserId);

            var allUsersId = leaderUsers
                .Concat(proposals.Select(_ => _.UserId))
                .Append(player.CreationUserId)
                .Distinct();

            var users = (await GetUsersFromIdsAsync(allUsersId).ConfigureAwait(false))
                .ToDictionary(_ => _.Id, _ => _);

            var leaderItems = leaders
                .Select(_ => new DayboardLeaderItem
                {
                    Date = _.CreationDate.Date,
                    IsCreator = false,
                    Points = _.Points,
                    Time = new TimeSpan(0, _.Time, 0),
                    UserId = _.UserId,
                    UserName = users[_.UserId].Login
                });

            if (users.ContainsKey(player.CreationUserId))
            {
                leaderItems = leaderItems.Append(new DayboardLeaderItem
                {
                    Date = day,
                    IsCreator = true,
                    Points = GetSubmittedPlayerPoints(leaders, day),
                    Time = new TimeSpan(23, 59, 59),
                    UserId = player.CreationUserId,
                    UserName = users[player.CreationUserId].Login
                });
            }

            switch (sort)
            {
                case DayLeaderSorts.BestTime:
                    leaderItems = leaderItems.SetPositions(_ => _.Time, false, (_, r) => _.Rank = r);
                    break;
                case DayLeaderSorts.TotalPoints:
                    leaderItems = leaderItems.SetPositions(_ => _.Points, true, (_, r) => _.Rank = r);
                    break;
            }

            var searchItems = proposals
                .Where(_ => !leaderUsers.Contains(_.UserId))
                .GroupBy(_ => _.UserId)
                .Select(_ => new DayboardSearcherItem
                {
                    Date = _.Select(p => p.CreationDate).Min().Date,
                    LastActivity = _.Select(p => p.CreationDate).Max(),
                    UserId = _.Key,
                    UserName = users[_.Key].Login
                })
                .OrderBy(_ => _.Date);

            return new Dayboard
            {
                Date = day,
                Sort = sort,
                Searchers = searchItems.ToList(),
                Leaders = leaderItems.ToList()
            };
        }

        private async Task<List<UserDto>> GetUsersFromIdsAsync(IEnumerable<ulong> allUsersId)
        {
            var users = new List<UserDto>();
            foreach (var userId in allUsersId)
            {
                var user = await _userRepository
                    .GetUserByIdAsync(userId)
                    .ConfigureAwait(false);

                if (user.UserTypeId != (ulong)UserTypes.Administrator)
                    users.Add(user);
            }

            return users;
        }

        private int GetSubmittedPlayerPoints(IEnumerable<LeaderDto> datesLeaders, DateTime date)
        {
            // ONLY DAY ONE COST POINTS
            var leadersCosting = ProposalChart.Default.SubmissionBonusPoints
                - datesLeaders
                    .Where(d => d.ProposalDate.Date == date
                        && d.Points >= ProposalChart.Default.SubmissionThresholdlosePoints
                        && d.IsCurrentDay)
                    .Sum(d => ProposalChart.Default.SubmissionLosePointsByLeader);

            return ProposalChart.Default.SubmissionBasePoints
                + Math.Max(leadersCosting, 0);
        }
    }
}
