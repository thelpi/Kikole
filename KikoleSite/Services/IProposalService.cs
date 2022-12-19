using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
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
        /// <param name="request">Proposal request.</param>
        /// <param name="userId">User idenfifier.</param>
        /// <param name="pInfo">Information about the player of the request.</param>
        /// <returns>Instance of <see cref="ProposalResponse"/>, proposals already made and leader info in case of win.</returns>
        Task<(ProposalResponse, IReadOnlyCollection<ProposalDto>, LeaderDto)> ManageProposalResponseAsync(ProposalRequest request, ulong userId, PlayerFullDto pInfo);

        /// <summary>
        /// Gets proposals for a specific date and user.
        /// </summary>
        /// <param name="proposalDate">Proposal date.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of proposals.</returns>
        Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(DateTime proposalDate, ulong userId);

        /// <summary>
        /// Checks grant access of a user for the specified day.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="date">Day to check.</param>
        /// <returns>Grant type.</returns>
        Task<DayGrantTypes> GetGrantAccessForDayAsync(ulong userId, DateTime date);
    }
}
