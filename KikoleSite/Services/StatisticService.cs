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
using KikoleSite.Models.Statistics;

namespace KikoleSite.Services
{
    public class StatisticService : IStatisticService
    {
        private readonly IStatisticRepository _statisticRepository;
        private readonly IInternationalRepository _internationalRepository;
        private readonly IClubRepository _clubRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly IClock _clock;

        public StatisticService(IStatisticRepository statisticRepository,
            IInternationalRepository internationalRepository,
            IClubRepository clubRepository,
            IPlayerRepository playerRepository,
            IUserRepository userRepository,
            ILeaderRepository leaderRepository,
            IProposalRepository proposalRepository,
            IClock clock)
        {
            _statisticRepository = statisticRepository;
            _internationalRepository = internationalRepository;
            _clubRepository = clubRepository;
            _playerRepository = playerRepository;
            _userRepository = userRepository;
            _leaderRepository = leaderRepository;
            _proposalRepository = proposalRepository;
            _clock = clock;
        }

        public async Task<PlayersDistribution> GetPlayersDistributionAsync(ulong userId, Languages language, int maxItemsRank)
        {
            var countriesPld = await _statisticRepository
                .GetPlayersDistributionByCountryAsync(userId)
                .ConfigureAwait(false);

            var decadesPld = await _statisticRepository
                .GetPlayersDistributionByDecadeAsync(userId)
                .ConfigureAwait(false);

            var clubsPld = await _statisticRepository
                .GetPlayersDistributionByClubAsync(userId)
                .ConfigureAwait(false);

            var positionsPld = await _statisticRepository
                .GetPlayersDistributionByPositionAsync(userId)
                .ConfigureAwait(false);

            var countries = await _internationalRepository
                .GetCountriesAsync((ulong)language)
                .ConfigureAwait(false);

            var clubs = await _clubRepository
                .GetClubsAsync()
                .ConfigureAwait(false);

            var totalCount = positionsPld.Sum(_ => _.Count);

            return new PlayersDistribution
            {
                TotalPlayersCount = totalCount,
                CountriesDistribution = ToDistributionItemsList(countriesPld, _ => new Country(countries.Single(c => c.Code == ((Countries)_).ToString())), totalCount, maxItemsRank),
                ClubsDistribution = ToDistributionItemsList(clubsPld, _ => new Club(clubs.Single(c => c.Id == _)), totalCount, maxItemsRank),
                DecadesDistribution = ToDistributionItemsList(decadesPld, _ => _, totalCount, -1),
                PositionsDistribution = ToDistributionItemsList(positionsPld, _ => (Positions)_, totalCount, -1)
            };
        }

        public async Task<ActiveUsers> GetActiveUsersAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var wDatas = await _statisticRepository
                .GetWeeklyActiveUsersAsync(startDate, endDate)
                .ConfigureAwait(false);

            var mDatas = await _statisticRepository
                .GetMonthlyActiveUsersAsync(startDate, endDate)
                .ConfigureAwait(false);

            var dDatas = await _statisticRepository
                .GetDailyActiveUsersAsync(startDate, endDate)
                .ConfigureAwait(false);

            return new ActiveUsers
            {
                DailyDatas = dDatas,
                MonthlyDatas = mDatas,
                WeeklyDatas = wDatas
            };
        }

        public async Task<IReadOnlyCollection<PlayerStatistics>> GetPlayersStatisticsAsync(ulong userId)
        {
            var players = await _playerRepository
                .GetPlayersOfTheDayAsync(null, _clock.Now)
                .ConfigureAwait(false);

            var knownPlayers = await _playerRepository
                .GetKnownPlayerNamesAsync(userId)
                .ConfigureAwait(false);

            var usersCache = new Dictionary<ulong, string>();

            var results = new List<PlayerStatistics>(players.Count);
            foreach (var p in players)
            {
                if (!usersCache.ContainsKey(p.CreationUserId))
                {
                    var creatorUser = await _userRepository
                        .GetUserByIdAsync(p.CreationUserId)
                        .ConfigureAwait(false);
                    usersCache.Add(p.CreationUserId, creatorUser.Login);
                }

                var proposals = await _proposalRepository
                    .GetProposalsAsync(p.ProposalDate.Value, false)
                    .ConfigureAwait(false);

                var leaders = await _leaderRepository
                    .GetLeadersAtDateAsync(p.ProposalDate.Value, false)
                    .ConfigureAwait(false);

                var ps = new PlayerStatistics
                {
                    Date = p.ProposalDate.Value,
                    Name = knownPlayers.Any(_ => _.Id == p.Id)
                        ? p.Name
                        : "N/A",
                    Creator = usersCache[p.CreationUserId],
                    TriesCountSameDay = proposals.Where(_ => _.IsCurrentDay).Select(_ => _.UserId).Distinct().Count(),
                    TriesCountTotal = proposals.Select(_ => _.UserId).Distinct().Count(),
                    BestTime = leaders.Count > 0
                        ? leaders.Min(_ => _.Time)
                        : 0,
                    SuccessesCountSameDay = leaders.Where(_ => _.IsCurrentDay).Count(),
                    SuccessesCountTotal = leaders.Count,
                    AveragePointsSameDay = leaders.Count(_ => _.IsCurrentDay) == 0
                        ? 0
                        : (int)leaders.Where(_ => _.IsCurrentDay).Average(_ => _.Points),
                    AveragePointsTotal = leaders.Count == 0
                        ? 0
                        : (int)leaders.Average(_ => _.Points)
                };

                results.Add(ps);
            }

            return results;
        }

        private static List<PlayersDistributionItem<T2>> ToDistributionItemsList<T1, T2>(
            IReadOnlyCollection<PlayersDistributionDto<T1>> sourceList,
            Func<T1, T2> transformItem, int totalCount, int maxItemsRank)
        {
            return sourceList
                .Select(_ =>
                {
                    return new PlayersDistributionItem<T2>(
                        transformItem(_.Value),
                        _.Count,
                        totalCount);
                })
                .SetPositions(_ => _.Rate, true, (_, i) => _.Rank = i)
                .TakeWhile(_ => maxItemsRank < 0 || _.Rank <= maxItemsRank)
                .ToList();
        }
    }
}
