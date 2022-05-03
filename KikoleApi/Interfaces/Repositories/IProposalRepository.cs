using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces.Repositories
{
    public interface IProposalRepository
    {
        Task<ulong> CreateProposalAsync(ProposalDto proposal);

        // loose (not necessarily on exact date)
        Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime playerProposalDate, ulong userId);

        Task<IReadOnlyCollection<ProposalDto>> GetProposalsDateExactAsync(DateTime playerProposalDate, ulong userId);

        Task<IReadOnlyCollection<ProposalDto>> GetAllProposalsDateExactAsync(ulong userId);

        Task<IReadOnlyCollection<ulong>> GetMissingUsersAsLeaderAsync(DateTime playerProposalDate);

        Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime playerProposalDate);
    }
}
