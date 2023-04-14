using System.Threading.Tasks;
using KikoleSite.Dtos;
using KikoleSite.Enums;

namespace KikoleSite.Repositories
{
    public interface IWriteRepository
    {
        Task<uint> ReplaceTimeEntryAsync(EntryDto requestEntry);

        Task<uint> InsertPlayerAsync(string urlName, string defaultHexColor);

        Task DeletePlayerEntriesAsync(Game game, uint playerId);

        Task UpdatePlayerAsync(PlayerDto player);

        Task BanPlayerAsync(uint playerId);

        Task DeleteEntriesAsync(params uint[] entriesId);
    }
}
