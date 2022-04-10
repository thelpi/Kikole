using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;

namespace KikoleApi.Interfaces.Services
{
    public interface IChallengeService
    {
        /// <summary>
        /// Gets challenge history for a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <returns>Collection of <see cref="Challenge"/>, sorted by date descending.</returns>
        Task<IReadOnlyCollection<Challenge>> GetChallengesHistoryAsync(ulong userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets details about accepted challenges for a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of <see cref="Challenge"/>, sorted by date descending.</returns>
        Task<IReadOnlyCollection<Challenge>> GetAcceptedChallengesAsync(ulong userId);

        /// <summary>
        /// Gets details about requested challenges for a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of <see cref="Challenge"/>.</returns>
        Task<IReadOnlyCollection<Challenge>> GetRequestedChallengesAsync(ulong userId);

        /// <summary>
        /// Gets details about pending challenges for a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of <see cref="Challenge"/>.</returns>
        Task<IReadOnlyCollection<Challenge>> GetPendingChallengesAsync(ulong userId);

        /// <summary>
        /// Responds to a challenge.
        /// </summary>
        /// <param name="id">Challenge identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="isAccepted">Challenge accepted y/n.</param>
        /// <returns>Error code.</returns>
        Task<ChallengeResponseError> RespondToChallengeAsync(ulong id, ulong userId, bool isAccepted);

        /// <summary>
        /// Creates a challenge.
        /// </summary>
        /// <param name="request">Challenge request.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Error code.</returns>
        Task<ChallengeResponseError> CreateChallengeAsync(ChallengeRequest request, ulong userId);
    }
}
