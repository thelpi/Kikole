using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Statistics;

namespace KikoleSite.Interfaces.Services
{
    public interface IStatisticService
    {
        Task<PlayersDistribution> GetPlayersDistributionAsync(ulong userId, Languages language, int maxItemsRank);

        Task<ActiveUsers> GetActiveUsersAsync(DateTime? startDate = null, DateTime? endDate = null);

        Task<IReadOnlyCollection<PlayerStatistics>> GetPlayersStatisticsAsync(ulong userId);
    }
}
