using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Interfaces.Repositories;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    /// <summary>
    /// Proposal repository implementation.
    /// </summary>
    /// <seealso cref="BaseRepository"/>
    /// <seealso cref="IProposalRepository"/>
    public class ProposalRepository : BaseRepository, IProposalRepository
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="clock">Clock service.</param>
        public ProposalRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        /// <inheritdoc />
        public async Task<ulong> CreateProposalAsync(ProposalDto proposal)
        {
            return await ExecuteInsertAsync(
                    "proposals",
                    ("user_id", proposal.UserId),
                    ("proposal_type_id", proposal.ProposalTypeId),
                    ("value", proposal.Value),
                    ("successful", proposal.Successful),
                    ("proposal_date", proposal.ProposalDate.Date),
                    ("days_before", proposal.DaysBefore),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(
            DateTime playerProposalDate, ulong userId)
        {
            return await GetProposalsInternalAsync(
                    "DATE(DATE_ADD(proposal_date, INTERVAL -days_before DAY)) = @real_proposal_date",
                    playerProposalDate,
                    userId)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<ProposalDto>> GetProposalsDateExactAsync(
            DateTime playerProposalDate, ulong userId)
        {
            return await GetProposalsInternalAsync(
                    "days_before = 0 AND DATE(proposal_date) = @real_proposal_date",
                    playerProposalDate,
                    userId)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<ProposalDto>> GetWiningProposalsAsync(DateTime playerProposalDate)
        {
            return await ExecuteReaderAsync<ProposalDto>(
                    "SELECT * FROM proposals " +
                    "WHERE proposal_type_id = @proposal_type_id " +
                    "AND successful > 0 AND days_before = 0 " +
                    "AND DATE(proposal_date) = @proposal_date " +
                    $"AND user_id IN ({SubSqlValidUsers})",
                    new
                    {
                        proposal_type_id = (ulong)ProposalTypes.Name,
                        proposal_date = playerProposalDate.Date
                    })
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<ProposalDto>> GetAllProposalsDateExactAsync(ulong userId)
        {
            return await GetDtosAsync<ProposalDto>(
                    "proposals",
                    ("user_id", userId),
                    ("days_before", 0))
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime playerProposalDate)
        {
            return await ExecuteReaderAsync<ProposalDto>(
                    "SELECT * FROM proposals " +
                    "WHERE days_before = 0 " +
                    "AND DATE(proposal_date) = @proposal_date " +
                    $"AND user_id IN ({SubSqlValidUsers})",
                    new
                    {
                        proposal_date = playerProposalDate.Date
                    })
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
