using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories
{
    public interface IProposalRepository
    {
        Task<ulong> CreateProposalAsync(ProposalDto proposal);

        // loose (not necessarily on exact date)
        Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime playerProposalDate, ulong userId);

        // loose (not necessarily on exact date)
        Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime playerProposalDateStart, DateTime playerProposalDateEnd, ulong userId);

        Task<IReadOnlyCollection<ProposalDto>> GetAllProposalsDateExactAsync(ulong userId);

        Task<IReadOnlyCollection<ulong>> GetMissingUsersAsLeaderAsync(DateTime playerProposalDate);

        Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime playerProposalDate, bool exact);

        Task<int> GetDaysCountWithProposalAsync(DateTime startDate, DateTime endDate, ulong userId, bool exact);

        Task<IReadOnlyCollection<ProposalDto>> GetProposalsActivityAsync();
    }
}
