using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Models;

namespace KikoleSite.Elite.Providers
{
    public interface IIntegrationProvider
    {
        Task ScanAllPlayersEntriesHistoryAsync(Game game);

        Task ScanPlayerEntriesHistoryAsync(Game game, long playerId);

        Task<IReadOnlyCollection<Player>> GetCleanableDirtyPlayersAsync();

        Task CheckPotentialBannedPlayersAsync();

        Task ScanTimePageForNewPlayersAsync(DateTime? stopAt, bool addEntries);

        Task<bool> CleanDirtyPlayerAsync(long playerId);
    }
}
