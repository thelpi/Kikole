using System;
using System.Threading.Tasks;
using KikoleSite.Dtos;
using KikoleSite.Enums;

namespace KikoleSite.Repositories
{
    public interface IWriteRepository
    {
        Task<long> ReplaceTimeEntryAsync(EntryDto requestEntry);

        Task<long> InsertPlayerAsync(string urlName, string defaultHexColor);

        Task DeletePlayerEntriesAsync(Game game, long playerId);

        Task UpdatePlayerAsync(PlayerDto player);

        Task BanPlayerAsync(long playerId);

        Task DeleteEntriesAsync(params long[] entriesId);
    }
}
