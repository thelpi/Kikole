using System;
using System.Collections.Generic;
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

        Task<uint> InsertRankingAsync(RankingDto ranking);

        Task InsertRankingEntriesAsync(IReadOnlyList<RankingEntryDto> rankingEntries);

        Task DeleteRankingAsync(uint id);

        Task DeletePlayerRankingsAsync(uint playerId);

        Task DeleteRankingsAsync(Stage stage, Level level, NoDateEntryRankingRule rule, DateTime? startDateInc, DateTime? endDateExc);
    }
}
