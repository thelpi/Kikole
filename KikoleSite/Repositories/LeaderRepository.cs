using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Interfaces;
using KikoleSite.Interfaces.Repositories;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories
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
                    ("creation_date", request.CreationDate))
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<LeaderDto>> GetLeadersAtDateAsync(DateTime date, bool onTimeOnly)
        {
            return await ExecuteReaderAsync<LeaderDto>(
                    "SELECT * FROM leaders " +
                    "WHERE proposal_date = @date " +
                    $"AND {(onTimeOnly ? $"proposal_date = DATE(creation_date)" : "1 = 1")} " +
                    $"AND user_id IN ({SubSqlValidUsers})",
                    new { date.Date })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(DateTime? minimalDate, DateTime? maximalDate, bool onTimeOnly)
        {
            return await ExecuteReaderAsync<LeaderDto>(
                    "SELECT * FROM leaders " +
                    "WHERE (@minimal_date IS NULL OR proposal_date >= @minimal_date) " +
                    "AND proposal_date <= IFNULL(@maximal_date, DATE(NOW())) " +
                    $"AND {(onTimeOnly ? $"proposal_date = DATE(creation_date)" : "1 = 1")} " +
                    $"AND user_id IN ({SubSqlValidUsers})",
                    new
                    {
                        minimal_date = minimalDate?.Date,
                        maximal_date = maximalDate?.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<LeaderDto>> GetUserLeadersAsync(DateTime? minimalDate, DateTime? maximalDate, bool onTimeOnly, ulong userId)
        {
            return await ExecuteReaderAsync<LeaderDto>(
                    "SELECT * FROM leaders " +
                    "WHERE (@minimal_date IS NULL OR proposal_date >= @minimal_date) " +
                    "AND proposal_date <= IFNULL(@maximal_date, DATE(NOW())) " +
                    $"AND {(onTimeOnly ? $"proposal_date = DATE(creation_date)" : "1 = 1")} " +
                    $"AND user_id = @userId ",
                    new
                    {
                        minimal_date = minimalDate?.Date,
                        maximal_date = maximalDate?.Date,
                        userId
                    })
                .ConfigureAwait(false);
        }
    }
}
