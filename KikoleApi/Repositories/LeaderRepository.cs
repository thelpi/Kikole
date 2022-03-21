﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models;
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

        public async Task<IReadOnlyCollection<LeaderDto>> GetLeadersAtDateAsync(DateTime date)
        {
            return await ExecuteReaderAsync<LeaderDto>(
                    $"SELECT * FROM leaders " +
                    $"WHERE DATE(proposal_date) = @date " +
                    $"AND user_id NOT IN (SELECT id FROM users WHERE is_disabled = 1 OR user_type_id = @adminId)",
                    new
                    {
                        date.Date,
                        adminId = (ulong)UserTypes.Administrator
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(DateTime? minimalDate)
        {
            return await ExecuteReaderAsync<LeaderDto>(
                    $"SELECT * FROM leaders " +
                    $"WHERE (@minimal_date IS NULL OR proposal_date >= @minimal_date) "+
                    $"AND user_id NOT IN (SELECT id FROM users WHERE is_disabled = 1 OR user_type_id = @adminId)",
                    new
                    {
                        minimal_date = minimalDate,
                        adminId = (ulong)UserTypes.Administrator
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<IReadOnlyCollection<LeaderDto>>> GetLeadersHistoryAsync(DateTime date)
        {
            var leadersHistory = new List<IReadOnlyCollection<LeaderDto>>();

            while (date.Date > ProposalChart.Default.FirstDate.Date)
            {
                date = date.AddDays(-1);
                var leadersBefore = await GetLeadersAtDateAsync(date)
                    .ConfigureAwait(false);
                leadersHistory.Add(leadersBefore);
            }

            return leadersHistory;
        }
    }
}
