using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    public class ProposalRepository : BaseRepository, IProposalRepository
    {
        public ProposalRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<ulong> CreateProposalAsync(ProposalDto proposal)
        {
            return await ExecuteInsertAsync(
                    "proposals",
                    ("user_id", proposal.UserId),
                    ("proposal_type_id", proposal.ProposalTypeId),
                    ("value", proposal.Value),
                    ("successful", proposal.Successful),
                    ("proposal_date", proposal.ProposalDate),
                    ("days_before", proposal.DaysBefore),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(
            DateTime proposalDate, ulong userId)
        {
            return await GetProposalsInternalAsync(
                    "DATE(DATE_ADD(proposal_date, INTERVAL -days_before DAY)) = @real_proposal_date",
                    proposalDate,
                    userId)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProposalDto>> GetProposalsDateExactAsync(
            DateTime proposalDate, ulong userId)
        {
            return await GetProposalsInternalAsync(
                    "days_before = 0 AND DATE(proposal_date) = @real_proposal_date",
                    proposalDate,
                    userId)
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyCollection<ProposalDto>> GetProposalsInternalAsync(
            string where, DateTime proposalDate, ulong userId)
        {
            return await ExecuteReaderAsync<ProposalDto>(
                    $"SELECT * FROM proposals WHERE user_id = @user_id AND {where}",
                    new
                    {
                        user_id = userId,
                        real_proposal_date = proposalDate.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProposalDto>> GetWiningProposalsAsync(DateTime proposalDate)
        {
            return await ExecuteReaderAsync<ProposalDto>(
                    "SELECT * FROM proposals WHERE proposal_type_id = @proposal_type_id AND successful > 0 AND days_before = 0 AND DATE(proposal_date) = @proposal_date",
                    new
                    {
                        proposal_type_id = (ulong)Models.ProposalTypes.Name,
                        proposal_date = proposalDate.Date
                    })
                .ConfigureAwait(false);
        }
    }
}
