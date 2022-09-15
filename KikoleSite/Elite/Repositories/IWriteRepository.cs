using System;
using System.Threading.Tasks;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Repositories
{
    public interface IWriteRepository
    {
        Task<long> ReplaceTimeEntryAsync(EntryDto requestEntry);

        Task<long> InsertPlayerAsync(string urlName, string defaultHexColor);

        Task DeletePlayerEntriesAsync(Game game, long playerId);

        Task UpdatePlayerAsync(PlayerDto player);

        Task BanPlayerAsync(long playerId);

        Task DeleteEntriesAsync(params long[] entriesId);

        Task<long> ReplaceRankingAsync(StageLevelRankingDto ranking);

        Task RemoveRankingsAfterDateAsync(Stage stage, Level level, DateTime dateMinInclude);
    }
}
