using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Interfaces.Services
{
    public interface IStatisticService
    {
        Task<PlayersDistribution> GetPlayersDistributionAsync(ulong userId, Languages language, int maxItemsRank);
        
        Task<IReadOnlyDictionary<int, int>> GetActivityDatasAsync();
    }
}
