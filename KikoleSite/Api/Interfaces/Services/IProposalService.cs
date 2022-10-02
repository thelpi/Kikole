using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Requests;

namespace KikoleSite.Api.Interfaces.Services
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
    }
}
