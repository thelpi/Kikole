using System.Threading.Tasks;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Repositories
{
    public interface IWriteRepository
    {
        Task<long> InsertTimeEntryAsync(EntryDto requestEntry);

        Task<long> InsertPlayerAsync(string urlName, string defaultHexColor);

        Task DeletePlayerEntriesAsync(Game game, long playerId);

        Task UpdatePlayerAsync(PlayerDto player);

        Task BanPlayerAsync(long playerId);
    }
}
