using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Repositories
{
    public interface ICacheManager
    {
        Task<IReadOnlyCollection<EntryDto>> GetStageLevelEntriesAsync(Stage stage, Level level);
        Task<IReadOnlyCollection<EntryDto>> GetPlayerEntriesAsync(Game game, long playerId);
        Task ToggleCacheLockAsync(bool newValue);
    }
}
