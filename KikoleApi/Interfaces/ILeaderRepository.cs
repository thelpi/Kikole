using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface ILeaderRepository
    {
        Task<ulong> CreateLeaderAsync(LeaderDto request);

        Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(DateTime? minimalDate);

        Task<IReadOnlyCollection<LeaderDto>> GetLeadersAtDateAsync(DateTime date);

        Task DeleteLeadersAsync(DateTime proposalDate);

        Task<IReadOnlyCollection<IReadOnlyCollection<LeaderDto>>> GetLeadersHistoryAsync(DateTime date);
    }
}
