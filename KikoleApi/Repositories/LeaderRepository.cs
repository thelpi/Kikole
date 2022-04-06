using System;
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
                    "SELECT * FROM leaders " +
                    "WHERE DATE(proposal_date) = @date " +
                    $"AND user_id IN ({SubSqlValidUsers})",
                    new { date.Date })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(DateTime? minimalDate, DateTime? maximalDate)
        {
            return await ExecuteReaderAsync<LeaderDto>(
                    "SELECT * FROM leaders " +
                    "WHERE (@minimal_date IS NULL OR proposal_date >= @minimal_date) " +
                    "AND proposal_date <= IFNULL(@maximal_date, DATE(NOW())) " +
                    $"AND user_id IN ({SubSqlValidUsers})",
                    new
                    {
                        minimal_date = minimalDate?.Date,
                        maximal_date = maximalDate?.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<IReadOnlyCollection<LeaderDto>>> GetLeadersHistoryAsync(DateTime date, DateTime firstDate)
        {
            var leadersHistory = new List<IReadOnlyCollection<LeaderDto>>();

            while (date.Date >= firstDate.Date)
            {
                var leadersBefore = await GetLeadersAtDateAsync(date)
                    .ConfigureAwait(false);
                leadersHistory.Add(leadersBefore);
                date = date.AddDays(-1);
            }

            return leadersHistory;
        }

        public async Task<IReadOnlyCollection<KikoleAwardDto>> GetKikoleAwardsAsync(DateTime minDate, DateTime maxDate)
        {
            return await ExecuteReaderAsync<KikoleAwardDto>(
                    "SELECT y.name, y.proposal_date, points / users_count AS avg_pts " +
                    "FROM (" +
                    "   SELECT proposal_date, SUM(points) AS points, (" +
                    "       SELECT COUNT(DISTINCT p.user_id) " +
                    "       FROM proposals AS p " +
                    "       WHERE p.days_before = 0 " +
                    "       AND DATE(p.proposal_date) = l.proposal_date" +
                    "   ) AS users_count " +
                    "   FROM leaders AS l " +
                    "   GROUP BY proposal_date" +
                    ") AS tmp " +
                    "JOIN players AS y ON tmp.proposal_date = y.proposal_date " +
                    "WHERE users_count >= 5 " +
                    "AND y.proposal_date >= @startdate " +
                    "AND y.proposal_date <= @enddate",
                    new
                    {
                        startdate = minDate.Date,
                        enddate = maxDate.Date
                    })
                .ConfigureAwait(false);
        }
    }
}
