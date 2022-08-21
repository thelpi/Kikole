using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KikoleSite.Elite
{
    public interface IReadRepository
    {
        Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync();
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate);
        Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayersAsync();
    }
}
