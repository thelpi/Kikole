using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Interfaces.Repositories
{
    public interface ILeaderRepository
    {
        Task<ulong> CreateLeaderAsync(LeaderDto request);

        Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(DateTime? minimalDate, DateTime? maximalDate, bool onTimeOnly);

        Task<IReadOnlyCollection<LeaderDto>> GetLeadersAtDateAsync(DateTime date, bool onTimeOnly);
    }
}
