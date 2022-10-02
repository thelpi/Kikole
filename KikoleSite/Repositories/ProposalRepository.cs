using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Interfaces;
using KikoleSite.Interfaces.Repositories;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories
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
                    ("proposal_date", proposal.ProposalDate.Date),
                    ("ip", proposal.Ip),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(
            DateTime playerProposalDate, ulong userId)
        {
            return await GetProposalsInternalAsync(
                    "proposal_date = @real_proposal_date",
                    playerProposalDate,
                    userId)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ulong>> GetMissingUsersAsLeaderAsync(DateTime playerProposalDate)
        {
            return await ExecuteReaderAsync<ulong>(
                    "SELECT DISTINCT user_id " +
                    "FROM proposals AS p " +
                    "WHERE proposal_type_id = @proposal_type_id " +
                    "AND successful > 0 " +
                    "AND proposal_date = @proposal_date " +
                    "AND NOT EXISTS (" +
                    "   SELECT 1 FROM leaders AS l " +
                    "   WHERE l.user_id = p.user_id " +
                    "   AND l.proposal_date = p.proposal_date " +
                    $") AND user_id IN ({SubSqlValidUsers})",
                    new
                    {
                        proposal_type_id = (ulong)ProposalTypes.Name,
                        proposal_date = playerProposalDate.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProposalDto>> GetAllProposalsDateExactAsync(ulong userId)
        {
            return await ExecuteReaderAsync<ProposalDto>(
                    "SELECT * FROM proposals " +
                    "WHERE user_id = @userId AND proposal_date = DATE(creation_date)",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime playerProposalDate, bool exact)
        {
            return await ExecuteReaderAsync<ProposalDto>(
                    "SELECT * FROM proposals " +
                    $"WHERE {(exact ? "proposal_date = DATE(creation_date)" : "1 = 1")} " +
                    "AND proposal_date = @proposal_date " +
                    $"AND user_id IN ({SubSqlValidUsers})",
                    new
                    {
                        proposal_date = playerProposalDate.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task<int> GetDaysCountWithProposalAsync(DateTime startDate, DateTime endDate, ulong userId, bool exact)
        {
            return await ExecuteScalarAsync(
                    "SELECT COUNT(DISTINCT proposal_date) " +
                    "FROM proposals " +
                    "WHERE user_id = @userId " +
                    "AND proposal_date >= @startDate " +
                    "AND proposal_date <= @endDate " +
                    "AND (proposal_date = DATE(creation_date) OR 1 = @loose)",
                    new
                    {
                        userId,
                        startDate = startDate.Date,
                        endDate = endDate.Date,
                        loose = exact ? 0 : 1
                    },
                    0)
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
    }
}
