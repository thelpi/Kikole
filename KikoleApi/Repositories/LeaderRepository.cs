﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    public class LeaderRepository : BaseRepository, ILeaderRepository
    {
        public LeaderRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<ulong> CreateLeaderAsync(LeaderDto request)
        {
            return await ExecuteInsertAsync(
                    "leaders",
                    ("user_id", request.UserId),
                    ("ip", request.Ip),
                    ("proposal_date", request.ProposalDate.Date),
                    ("points", request.Points),
                    ("time", request.Time),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task DeleteLeadersAsync(DateTime proposalDate)
        {
            await ExecuteNonQueryAsync(
                    "DELETE FROM leaders WHERE proposal_date = @proposal_date",
                    new { proposal_date = proposalDate.Date })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(
            DateTime? minimalDate, bool includeAnonymous)
        {
            return await ExecuteReaderAsync<LeaderDto>(
                    $"SELECT * FROM leaders " +
                    $"WHERE (IFNULL(user_id, 0) > 0 OR 1 = @anonymous) " +
                    $"AND (@minimal_date IS NULL OR proposal_date >= @minimal_date) "+
                    $"AND user_id NOT IN (SELECT id FROM users WHERE is_disabled = 1 OR is_admin = 1)",
                    new
                    {
                        anonymous = includeAnonymous ? 1 : 0,
                        minimal_date = minimalDate
                    })
                .ConfigureAwait(false);
        }

        public async Task UpdateLeadersUserAsync(ulong userId, string ip)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE leaders SET user_id = @user_id WHERE IFNULL(user_id, 0) = 0 AND ip = @ip",
                    new { ip, user_id = userId })
                .ConfigureAwait(false);
        }
    }
}
