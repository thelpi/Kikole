using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Helpers;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Statistics;
using KikoleSite.Repositories;

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

        public async Task<IReadOnlyCollection<PlayerStatistics>> GetPlayersStatisticsAsync(ulong userId, string anonymizedName, PlayerSorts sort, bool desc)
        {
            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            var isAdmin = user?.UserTypeId == (ulong)UserTypes.Administrator;

            var players = await _playerRepository
                .GetPlayersOfTheDayAsync(null, _clock.Yesterday)
                .ConfigureAwait(false);

            var allLeaders = await _leaderRepository
                .GetLeadersAsync(null, null, false)
                .ConfigureAwait(false);

            var creatorUsersId = players.Select(_ => _.CreationUserId).Distinct();
            var usersCache = new Dictionary<ulong, string>(players.Count);
            foreach (var creatorUserId in creatorUsersId)
            {
                var creatorUser = await _userRepository
                    .GetUserByIdAsync(creatorUserId)
                    .ConfigureAwait(false);
                usersCache.Add(creatorUserId, creatorUser.Login);
            }

            var allProposals = await _proposalRepository
                .GetProposalsActivityAsync()
                .ConfigureAwait(false);

            var results = players
                .Select(p =>
                {
                    var proposals = allProposals.Where(_ => _.ProposalDate == p.ProposalDate.Value);

                    var leaders = allLeaders.Where(_ => _.ProposalDate == p.ProposalDate.Value);

                    return new PlayerStatistics
                    {
                        Date = p.ProposalDate.Value,
                        Name = leaders.Any(_ => _.UserId == userId) || userId == p.CreationUserId || isAdmin
                            ? p.Name
                            : anonymizedName,
                        Creator = usersCache[p.CreationUserId],
                        TriesCountSameDay = proposals.Where(_ => _.IsCurrentDay).Select(_ => _.UserId).Distinct().Count(),
                        TriesCountTotal = proposals.Select(_ => _.UserId).Distinct().Count(),
                        BestTime = leaders.Any()
                            ? leaders.Min(_ => _.Time)
                            : 0,
                        SuccessesCountSameDay = leaders.Where(_ => _.IsCurrentDay).Count(),
                        SuccessesCountTotal = leaders.Count(),
                        AveragePointsSameDay = leaders.Count(_ => _.IsCurrentDay) == 0
                            ? 0
                            : (int)leaders.Where(_ => _.IsCurrentDay).Average(_ => _.Points),
                        AveragePointsTotal = leaders.Any()
                            ? (int)leaders.Average(_ => _.Points)
                            : 0,
                        DaysBefore = (int)(_clock.Now - p.ProposalDate.Value).TotalDays
                    };
                });

            switch (sort)
            {
                case PlayerSorts.AttempsCountOverall:
                    results = results.OrderBy(_ => _.TriesCountTotal);
                    break;
                case PlayerSorts.AttempsCountSameDay:
                    results = results.OrderBy(_ => _.TriesCountSameDay);
                    break;
                case PlayerSorts.BestTime:
                    results = results.OrderBy(_ => _.BestTime);
                    break;
                case PlayerSorts.CreatorLogin:
                    results = results.OrderBy(_ => _.Creator);
                    break;
                case PlayerSorts.LeadersCountOverall:
                    results = results.OrderBy(_ => _.SuccessesCountTotal);
                    break;
                case PlayerSorts.LeadersCountSameDay:
                    results = results.OrderBy(_ => _.SuccessesCountSameDay);
                    break;
                case PlayerSorts.Name:
                    results = results.OrderBy(_ => _.Name);
                    break;
                case PlayerSorts.PointsOverall:
                    results = results.OrderBy(_ => _.AveragePointsTotal);
                    break;
                case PlayerSorts.PointsSameDay:
                    results = results.OrderBy(_ => _.AveragePointsSameDay);
                    break;
                case PlayerSorts.ProposalDate:
                    results = results.OrderBy(_ => _.Date);
                    break;
            }

            if (desc)
                results = results.Reverse();

            return results.ToList();
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
