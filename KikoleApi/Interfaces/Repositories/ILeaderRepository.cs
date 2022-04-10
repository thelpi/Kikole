using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces.Repositories
{
    public interface ILeaderRepository
    {
        Task<ulong> CreateLeaderAsync(LeaderDto request);

        Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(DateTime? minimalDate, DateTime? maximalDate);

        Task<IReadOnlyCollection<LeaderDto>> GetLeadersAtDateAsync(DateTime date);

        Task DeleteLeadersAsync(DateTime proposalDate);

        Task<IReadOnlyCollection<KikoleAwardDto>> GetKikoleAwardsAsync(DateTime minDate, DateTime maxDate);
    }
}
