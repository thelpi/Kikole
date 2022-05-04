using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
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
