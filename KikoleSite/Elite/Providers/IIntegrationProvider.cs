using System;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Providers
{
    public interface IIntegrationProvider
    {
        Task RefreshAllEntriesAsync(Game game);

        Task RefreshEntriesToDateAsync(DateTime stopAt);

        Task RefreshPlayersAsync();
    }
}
