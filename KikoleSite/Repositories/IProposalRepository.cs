using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IProposalRepository
{
    Task<ulong> CreateProposalAsync(ProposalDto proposal);

    // loose (not necessarily on exact date)
    Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateOnly playerProposalDate, ulong userId);

    // loose (not necessarily on exact date)
    Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateOnly playerProposalDateStart, DateOnly playerProposalDateEnd, ulong userId);

    Task<IReadOnlyCollection<ProposalDto>> GetAllProposalsDateExactAsync(ulong userId);

    Task<IReadOnlyCollection<ulong>> GetMissingUsersAsLeaderAsync(DateOnly playerProposalDate);

    Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateOnly playerProposalDate, bool exact);

    Task<int> GetDaysCountWithProposalAsync(DateOnly startDate, DateOnly endDate, ulong userId, bool exact);

    Task<IReadOnlyCollection<ProposalDto>> GetProposalsActivityAsync();
}
