using System;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Providers
{
    public interface IIntegrationProvider
    {
        Task ScanAllPlayersEntriesHistoryAsync(Game game);

        Task ScanPlayerEntriesHistoryAsync(Game game, long playerId);

        Task ScanTimePageForNewPlayersAsync(DateTime? stopAt, bool addEntries);

        Task RefreshPlayersAsync();
    }
}
