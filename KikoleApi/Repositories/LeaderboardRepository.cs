using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    public class LeaderboardRepository : BaseRepository, ILeaderboardRepository
    {
        public LeaderboardRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<ulong> CreateLeaderboardAsync(LeaderboardDto request)
        {
            return await ExecuteInsertAsync(
                    "leaderboards",
                    ("user_id", request.UserId),
                    ("ip", request.Ip),
                    ("proposal_date", request.ProposalDate.Date),
                    ("points", request.Points),
                    ("time", request.Time),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<LeaderboardDto>> GetLeaderboardsAsync(
            DateTime? minimalDate, bool includeAnonymous)
        {
            return await ExecuteReaderAsync<LeaderboardDto>(
                    $"SELECT * FROM leaderboards" +
                    $"WHERE (IFNULL(user_id, 0) > 0 OR 1 = @anonymous)" +
                    $"AND (@minimal_date IS NULL OR proposal_date >= @minimal_date)"+
                    $"AND user_id NOT IN (SELECT id FROM users WHERE is_disabled = 1 OR is_admin = 1)",
                    new
                    {
                        anonymous = includeAnonymous ? 1 : 0,
                        minimal_date = minimalDate
                    })
                .ConfigureAwait(false);
        }

        public async Task UpdateLeaderboardsUserAsync(ulong userId, string ip)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE leaderboards SET user_id = @user_id WHERE IFNULL(user_id, 0) = 0 AND ip = @ip",
                    new { ip, user_id = userId })
                .ConfigureAwait(false);
        }
    }
}
