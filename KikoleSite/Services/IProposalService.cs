using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Requests;

namespace KikoleSite.Services
{
    /// <summary>
    /// Proposal service interface.
    /// </summary>
    public interface IProposalService
    {
        /// <summary>
        /// Prepares a response to a proposal request.
        /// </summary>
        /// <typeparam name="T">Subtype of the proposal request.</typeparam>
        /// <param name="request">Proposal request.</param>
        /// <param name="userId">User idenfifier.</param>
        /// <param name="pInfo">Information about the player of the request.</param>
        /// <returns>Instance of <see cref="ProposalResponse"/>, proposals already made and leader info in case of win.</returns>
        Task<(ProposalResponse, IReadOnlyCollection<ProposalDto>, LeaderDto)> ManageProposalResponseAsync<T>(T request, ulong userId, PlayerFullDto pInfo)
            where T : BaseProposalRequest;

        /// <summary>
        /// Gets proposals for a specific date and user.
        /// </summary>
        /// <param name="proposalDate">Proposal date.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of proposals.</returns>
        Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(DateTime proposalDate, ulong userId);

        /// <summary>
        /// Checks if an user can see the daily leaderboard today.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>
        /// <c>True</c> if leaderboard is available; 4 ways:
        /// <list type="bullet">
        /// <item>User is administrator.</item>
        /// <item>User has found the player today.</item>
        /// <item>User has requested the leaderboard (against points).</item>
        /// <item>User has created today's player.</item>
        /// </list>
        /// </returns>
        Task<bool> CanSeeTodayLeaderboardAsync(ulong userId);
    }
}
