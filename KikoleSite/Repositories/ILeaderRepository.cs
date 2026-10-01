using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface ILeaderRepository
{
    Task<ulong> CreateLeaderAsync(LeaderDto request);

    Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(DateOnly? minimalDate, DateOnly? maximalDate, bool onTimeOnly);

    Task<IReadOnlyCollection<LeaderDto>> GetUserLeadersAsync(DateOnly? minimalDate, DateOnly? maximalDate, bool onTimeOnly, ulong userId);

    Task<IReadOnlyCollection<LeaderDto>> GetLeadersAtDateAsync(DateOnly date, bool onTimeOnly);
}
