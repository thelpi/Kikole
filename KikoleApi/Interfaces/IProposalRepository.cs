using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IProposalRepository
    {
        Task<ulong> CreateProposalAsync(ProposalDto proposal);

        Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime proposalDate, ulong userId);

        Task<IReadOnlyCollection<ProposalDto>> GetProposalsDateExactAsync(DateTime proposalDate, ulong userId);

        Task<IReadOnlyCollection<ProposalDto>> GetAllProposalsDateExactAsync(ulong userId);

        Task<IReadOnlyCollection<ProposalDto>> GetWiningProposalsAsync(DateTime proposalDate);
    }
}
