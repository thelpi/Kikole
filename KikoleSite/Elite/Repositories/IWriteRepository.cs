using System.Threading.Tasks;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Repositories
{
    public interface IWriteRepository
    {
        Task<long> InsertTimeEntryAsync(EntryDto requestEntry);

        Task<long> InsertPlayerAsync(string urlName, string defaultHexColor);

        Task DeletePlayerStageEntriesAsync(Stage stage, long playerId);

        Task UpdateDirtyPlayerAsync(long playerId);

        Task CleanPlayerAsync(PlayerDto player);
    }
}
