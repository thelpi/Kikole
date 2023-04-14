using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Dtos;
using KikoleSite.Enums;

namespace KikoleSite.Repositories
{
    public interface IReadRepository
    {
        Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync(bool banned = false, bool fromCache = false);
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate);
        Task<IReadOnlyCollection<EntryDto>> GetPlayerEntriesAsync(uint playerId, Game game);
    }
}
