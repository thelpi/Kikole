using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IChallengeRepository
    {
        Task<ulong> CreateChallengeAsync(ChallengeDto challenge);

        Task RespondToChallengeAsync(ulong challengeId, bool accept);

        Task<IReadOnlyCollection<ChallengeDto>> GetAcceptedChallengesOfTheDayAsync(DateTime date);

        Task<IReadOnlyCollection<ChallengeDto>> GetAcceptedChallengesAsync(DateTime? startDate, DateTime? endDate);

        Task<IReadOnlyCollection<ChallengeDto>> GetPendingChallengesByGuestUserAsync(ulong userId);

        Task<IReadOnlyCollection<ChallengeDto>> GetChallengesByUserAndByDateAsync(ulong userId, DateTime date);

        Task<IReadOnlyCollection<ChallengeDto>> GetRequestedAcceptedChallengesAsync(ulong userId, DateTime startDate, DateTime endDate);

        Task<IReadOnlyCollection<ChallengeDto>> GetResponseAcceptedChallengesAsync(ulong userId, DateTime startDate, DateTime endDate);

        Task<ChallengeDto> GetChallengeByIdAsync(ulong id);
    }
}
