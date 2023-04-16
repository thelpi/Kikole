using System;
using System.Threading.Tasks;
using KikoleSite.Enums;
using KikoleSite.Models.Integration;

namespace KikoleSite.Providers
{
    public interface IIntegrationProvider
    {
        Task<RefreshEntriesResult> RefreshAllEntriesAsync(Game game);

        Task<RefreshEntriesResult> RefreshPlayerEntriesAsync(uint playerId);

        Task<RefreshEntriesResult> RefreshEntriesToDateAsync(DateTime stopAt);

        Task<RefreshPlayersResult> RefreshPlayersAsync(bool addTimesForNewPlayers, bool refreshExistingPlayers);

        Task<RefreshRankingsResult> ComputeRankingsAsync(Game game, DateTime? startDate);
    }
}
