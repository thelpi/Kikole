using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Helpers;
using KikoleSite.Interfaces;
using KikoleSite.Interfaces.Repositories;
using KikoleSite.Interfaces.Services;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Services
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

            var items = await ComputeLeaderboardItemsAsync(
                    startDate, endDate, onTimeOnly)
                .ConfigureAwait(false);

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

        private async Task<List<LeaderboardItem>> ComputeLeaderboardItemsAsync(DateTime startDate, DateTime endDate, bool onTimeOnly)
        {
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

            return items;
        }

        /// <inheritdoc />
        public async Task<UserStat> GetUserStatisticsAsync(ulong userId, ulong requestUserId, string anonymizedName)
        {
            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return null;

            var requestUser = await _userRepository
                .GetUserByIdAsync(requestUserId)
                .ConfigureAwait(false);

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

                var pName = pDay.Name;

                if (requestUserId == 0)
                    pName = anonymizedName;
                else if (!leaders.Any(_ => _.UserId == requestUserId) && pDay.CreationUserId != requestUserId)
                {
                    if (!(requestUser?.UserTypeId == (ulong)UserTypes.Administrator))
                        pName = anonymizedName;
                }

                var singleStat = isCreator
                    ? new DailyUserStat(currentDate, pName, GetSubmittedPlayerPoints(leaders, currentDate))
                    : new DailyUserStat(userId, currentDate, pName, proposals.Any(_ => _.IsCurrentDay), proposals.Count > 0, leaders, meLeader);

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

        public async Task<Palmares> GetPalmaresAsync()
        {
            var months = new Dictionary<(int month, int year), (User first, User second, User third)>();

            var users = new Dictionary<ulong, (User, int, int, int)>();

            var date = await _playerRepository
                .GetFirstDateAsync()
                .ConfigureAwait(false);
            date = new DateTime(date.Year, date.Month, 1);

            var currentMonth = new DateTime(_clock.Today.Year, _clock.Today.Month, 1);
            while (date <= currentMonth)
            {
                var nextMonth = date.AddMonths(1);

                var ldItems = await ComputeLeaderboardItemsAsync(
                        new DateTime(date.Year, date.Month, 1),
                        nextMonth.AddDays(-1),
                        true)
                    .ConfigureAwait(false);

                var orderedLdItems = ldItems
                    .OrderByDescending(x => x.Points)
                    .ThenByDescending(x => x.KikolesFound)
                    .ThenBy(x => x.BestTime)
                    .ToList();

                var first = orderedLdItems[0];
                User firstUser = null;
                if (!users.ContainsKey(first.UserId))
                {
                    firstUser = new User(
                        new UserDto
                        {
                            Id = first.UserId,
                            Login = first.UserName
                        });
                    users.Add(first.UserId, (firstUser, 1, 0, 0));
                }
                else
                {
                    firstUser = users[first.UserId].Item1;
                    users[first.UserId] = (
                        firstUser,
                        users[first.UserId].Item2 + 1,
                        users[first.UserId].Item3,
                        users[first.UserId].Item4);
                }

                var second = orderedLdItems[1];
                User secondUser = null;
                if (!users.ContainsKey(second.UserId))
                {
                    secondUser = new User(
                        new UserDto
                        {
                            Id = second.UserId,
                            Login = second.UserName
                        });
                    users.Add(second.UserId, (secondUser, 0, 1, 0));
                }
                else
                {
                    secondUser = users[second.UserId].Item1;
                    users[second.UserId] = (
                        secondUser,
                        users[second.UserId].Item2,
                        users[second.UserId].Item3 + 1,
                        users[second.UserId].Item4);
                }

                var third = orderedLdItems[2];
                User thirdUser = null;
                if (!users.ContainsKey(third.UserId))
                {
                    thirdUser = new User(
                        new UserDto
                        {
                            Id = third.UserId,
                            Login = third.UserName
                        });
                    users.Add(third.UserId, (thirdUser, 0, 0, 1));
                }
                else
                {
                    thirdUser = users[third.UserId].Item1;
                    users[third.UserId] = (
                        thirdUser,
                        users[third.UserId].Item2,
                        users[third.UserId].Item3,
                        users[third.UserId].Item4 + 1);
                }

                months.Add((date.Month, date.Year), (
                    firstUser,
                    secondUser,
                    thirdUser));

                date = nextMonth;
            }

            return new Palmares
            {
                MonthlyPalmares = months,
                GlobalPalmares = users.Values
                    .Select(x => x)
                    .OrderByDescending(x => x.Item2)
                    .ThenByDescending(x => x.Item3)
                    .ThenByDescending(x => x.Item4)
                    .ToList()
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
