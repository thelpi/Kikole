using System;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Models.Integration;

namespace KikoleSite.Elite.Providers
{
    public interface IIntegrationProvider
    {
        Task RefreshAllEntriesAsync(Game game);

        Task RefreshEntriesToDateAsync(DateTime stopAt);

        Task<RefreshPlayersResult> RefreshPlayersAsync();
    }
}
