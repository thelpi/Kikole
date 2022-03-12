using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface ILeaderboardRepository
    {
        Task<ulong> CreateLeaderboardAsync(LeaderboardDto request);

        Task UpdateLeaderboardsUserAsync(ulong userId, string ip);

        Task<IReadOnlyCollection<LeaderboardDto>> GetLeaderboardsAsync(System.DateTime? minimalDate, bool includeAnonymous);
    }
}
