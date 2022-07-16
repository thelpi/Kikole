using System;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
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
        private readonly IUserRepository _userRepository;
        private readonly IClock _clock;

        public StatisticService(IStatisticRepository statisticRepository,
            IUserRepository userRepository,
            IClock clock)
        {
            _statisticRepository = statisticRepository;
            _userRepository = userRepository;
            _clock = clock;
        }

        public async Task<PlayersDistribution> GetPlayersDistributionAsync(ulong userId, Languages language, int maxItemsCount)
        {
            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            var endDate = user.UserTypeId == (ulong)UserTypes.Administrator
                ? default(DateTime?)
                : _clock.Today;

            var pld = new PlayersDistribution();

            var countriesPld = await _statisticRepository
                .GetPlayersDistributionByCountryAsync((ulong)language, null, endDate)
                .ConfigureAwait(false);

            var decadesPld = await _statisticRepository
                .GetPlayersDistributionByDecadeAsync(null, endDate)
                .ConfigureAwait(false);

            var clubsPld = await _statisticRepository
                .GetPlayersDistributionByClubAsync(null, endDate)
                .ConfigureAwait(false);

            var positionsPld = await _statisticRepository
                .GetPlayersDistributionByPositionAsync(null, endDate)
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
