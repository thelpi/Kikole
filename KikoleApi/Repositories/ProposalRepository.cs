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
                    ("ip", proposal.Ip),
                    ("proposal_type_id", proposal.ProposalTypeId),
                    ("value", proposal.Value),
                    ("successful", proposal.Successful),
                    ("proposal_date", proposal.ProposalDate),
                    ("days_before", proposal.DaysBefore),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(
            DateTime proposalDate, ulong userId, string ip)
        {
            string where;
            object prms;
            if (userId == 0)
            {
                where = "ip = @ip";
                prms = new
                {
                    ip,
                    real_proposal_date = proposalDate.Date
                };
            }
            else
            {
                where = "user_id = @user_id";
                prms = new
                {
                    user_id = userId,
                    real_proposal_date = proposalDate.Date
                };
            }

            return await ExecuteReaderAsync<ProposalDto>(
                    $"SELECT * FROM proposals WHERE {where} AND DATE(DATE_ADD(proposal_date, INTERVAL -days_before DAY)) = @real_proposal_date",
                    prms)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProposalDto>> GetWiningProposalsAsync(DateTime proposalDate)
        {
            return await ExecuteReaderAsync<ProposalDto>(
                    "SELECT * FROM proposals WHERE proposal_type_id = @proposal_type_id AND successful > 0 AND days_before = 0 AND DATE(proposal_date) = @proposal_date",
                    new
                    {
                        proposal_type_id = (ulong)Models.ProposalType.Name,
                        proposal_date = proposalDate.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task UpdateProposalsUserAsync(ulong userId, string ip)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE proposals SET user_id = @user_id WHERE IFNULL(user_id, 0) = 0 AND ip = @ip",
                    new { ip, user_id = userId })
                .ConfigureAwait(false);
        }
    }
}
