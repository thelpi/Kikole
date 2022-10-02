using System;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Enums;
using KikoleSite.Api.Models.Statistics;

namespace KikoleSite.Api.Interfaces.Services
{
    public interface IStatisticService
    {
        Task<PlayersDistribution> GetPlayersDistributionAsync(ulong userId, Languages language, int maxItemsRank);

        Task<ActiveUsers> GetActiveUsersAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
