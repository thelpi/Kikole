using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Requests;

namespace KikoleApi.Interfaces.Services
{
    public interface IProposalService
    {
        /// <summary>
        /// Prepares a response to a proposal request.
        /// </summary>
        /// <typeparam name="T">Subtype of the proposal request.</typeparam>
        /// <param name="request">Proposal request.</param>
        /// <param name="userId">User idenfifier.</param>
        /// <param name="pInfo">Information about the player of the request.</param>
        /// <returns>Instance of <see cref="ProposalResponse"/>.</returns>
        Task<ProposalResponse> ManageProposalResponseAsync<T>(T request, ulong userId, PlayerFullDto pInfo)
            where T : BaseProposalRequest;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proposalDate"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(DateTime proposalDate, ulong userId);
    }
}
