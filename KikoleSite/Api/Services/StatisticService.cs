using System.Linq;
using System.Threading.Tasks;
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

        public StatisticService(IStatisticRepository statisticRepository)
        {
            _statisticRepository = statisticRepository;
        }

        public async Task<PlayersDistribution> GetPlayersDistributionAsync(ulong userId, Languages language, int maxItemsCount)
        {
            var pld = new PlayersDistribution();

            var countriesPld = await _statisticRepository
                .GetPlayersDistributionByCountryAsync(userId, (ulong)language)
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

            pld.CountriesDistribution = countriesPld
                .Take(maxItemsCount)
                .ToList();

            pld.ClubsDistribution = clubsPld
                .Take(maxItemsCount)
                .ToList();

            pld.DecadesDistribution = decadesPld
                .Take(maxItemsCount)
                .Select(_ =>
                    new PlayersDistributionDto<string>
                    {
                        Count = _.Count,
                        Value = _.Value.ToString().PadRight(4, '0')
                    })
                .ToList();

            pld.PositionsDistribution = positionsPld
                .Take(maxItemsCount)
                .Select(_ =>
                    new PlayersDistributionDto<Positions>
                    {
                        Count = _.Count,
                        Value = (Positions)_.Value 
                    })
                .ToList();

            return pld;
        }
    }
}
