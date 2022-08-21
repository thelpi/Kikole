using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Repositories
{
    public interface IReadRepository
    {
        Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync();
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate);
        Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayersAsync();
    }
}
