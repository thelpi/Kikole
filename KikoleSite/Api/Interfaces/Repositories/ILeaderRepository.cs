using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
{
    public interface ILeaderRepository
    {
        Task<ulong> CreateLeaderAsync(LeaderDto request);

        Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(DateTime? minimalDate, DateTime? maximalDate, bool onTimeOnly);

        Task<IReadOnlyCollection<LeaderDto>> GetLeadersAtDateAsync(DateTime date, bool onTimeOnly);
    }
}
