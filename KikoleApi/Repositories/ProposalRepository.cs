﻿using System.Threading.Tasks;
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
    }
}
