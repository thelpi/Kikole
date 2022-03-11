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
            return await ExecuteReaderAsync<ProposalDto>(
                    "SELECT * FROM proposals WHERE user_id = @user_id AND DATE(DATE_ADD(proposal_date, INTERVAL -days_before DAY)) = @real_proposal_date",
                    new
                    {
                        user_id = userId,
                        real_proposal_date = proposalDate.Date
                    })
                .ConfigureAwait(false);
        }
    }
}
