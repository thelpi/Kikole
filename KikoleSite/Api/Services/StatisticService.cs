using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Helpers;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Interfaces.Services;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Services
{
    public class StatisticService : IStatisticService
    {
        private readonly IStatisticRepository _statisticRepository;
        private readonly IInternationalRepository _internationalRepository;
        private readonly IClubRepository _clubRepository;

        public StatisticService(IStatisticRepository statisticRepository,
            IInternationalRepository internationalRepository,
            IClubRepository clubRepository)
        {
            _statisticRepository = statisticRepository;
            _internationalRepository = internationalRepository;
            _clubRepository = clubRepository;
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
                DecadesDistribution = ToDistributionItemsList(decadesPld, _ => _, totalCount, maxItemsRank),
                PositionsDistribution = ToDistributionItemsList(positionsPld, _ => (Positions)_, totalCount, maxItemsRank)
            };
        }

        public async Task<IReadOnlyDictionary<int, int>> GetActivityDatasAsync()
        {
            return await _statisticRepository
                .GetActivityDatasAsync()
                .ConfigureAwait(false);
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
                .TakeWhile(_ => _.Rank <= maxItemsRank)
                .ToList();
        }
    }
}
