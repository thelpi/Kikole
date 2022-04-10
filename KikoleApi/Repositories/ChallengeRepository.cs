using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Interfaces.Repositories;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    public class ChallengeRepository : BaseRepository, IChallengeRepository
    {
        public ChallengeRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<ulong> CreateChallengeAsync(ChallengeDto challenge)
        {
            return await ExecuteInsertAsync("challenges",
                    ("guest_user_id", challenge.GuestUserId),
                    ("host_user_id", challenge.HostUserId),
                    ("points_rate", challenge.PointsRate),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task RespondToChallengeAsync(ulong challengeId, bool accept, DateTime date)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE challenges " +
                    "SET is_accepted = @isAccepted, challenge_date = @date " +
                    "WHERE id = @challengeId",
                    new
                    {
                        challengeId,
                        isAccepted = accept ? 1 : 0,
                        date = date.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetAcceptedChallengesOfTheDayAsync(DateTime date)
        {
            return await GetAcceptedChallengesAsync(date, date)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetAcceptedChallengesAsync(DateTime? startDate, DateTime? endDate)
        {
            return await ExecuteReaderAsync<ChallengeDto>(
                   "SELECT * FROM challenges " +
                   "WHERE is_accepted = 1 " +
                   "AND (@startDate IS NULL OR challenge_date >= @startDate) " +
                   "AND (@endDate IS NULL OR challenge_date <= @endDate) " +
                   $"AND host_user_id IN ({SubSqlValidUsers}) " +
                   $"AND guest_user_id IN ({SubSqlValidUsers})",
                   new
                   {
                       startDate = startDate?.Date,
                       endDate = endDate?.Date
                   })
               .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetPendingChallengesByGuestUserAsync(ulong userId)
        {
            return await ExecuteReaderAsync<ChallengeDto>(
                    "SELECT * FROM challenges " +
                    "WHERE guest_user_id = @userId " +
                    "AND is_accepted IS NULL " +
                    $"AND host_user_id IN ({SubSqlValidUsers})",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<ChallengeDto> GetChallengeByIdAsync(ulong id)
        {
            return await GetDtoAsync<ChallengeDto>(
                    "challenges",
                    ("id", id))
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetRequestedAcceptedChallengesAsync(
            ulong userId, DateTime startDate, DateTime endDate)
        {
            return await GetAcceptedChallengesAsync(
                    userId, startDate, endDate, "host_user_id", "guest_user_id")
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetResponseAcceptedChallengesAsync(
            ulong userId, DateTime startDate, DateTime endDate)
        {
            return await GetAcceptedChallengesAsync(
                    userId, startDate, endDate, "guest_user_id", "host_user_id")
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyCollection<ChallengeDto>> GetAcceptedChallengesAsync(
            ulong userId, DateTime startDate, DateTime endDate, string column1, string column2)
        {
            return await ExecuteReaderAsync<ChallengeDto>(
                   "SELECT * FROM challenges " +
                   "WHERE is_accepted = 1 " +
                   "AND challenge_date <= @endDate " +
                   "AND challenge_date >= @startDate " +
                   $"AND {column1} = @userId " +
                   $"AND {column2} IN ({SubSqlValidUsers})",
                   new
                   {
                       userId,
                       startDate = startDate.Date,
                       endDate = endDate.Date
                   })
               .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetPendingChallengesByHostUserAsync(ulong userId)
        {
            return await ExecuteReaderAsync<ChallengeDto>(
                    "SELECT * FROM challenges " +
                    "WHERE host_user_id = @userId " +
                    "AND is_accepted IS NULL " +
                    $"AND guest_user_id IN ({SubSqlValidUsers})",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetUsersFutureChallengesAsync(ulong hostUserId, ulong guestUserId)
        {
            return await ExecuteReaderAsync<ChallengeDto>(
                    "SELECT * FROM challenges " +
                    "WHERE challenge_date > DATE(NOW()) " +
                    "AND guest_user_id = @guestUserId " +
                    "AND host_user_id = @hostUserId",
                    new { guestUserId, hostUserId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<DateTime>> GetBookedChallengesAsync(ulong userId)
        {
            return await ExecuteReaderAsync<DateTime>(
                    "SELECT challenge_date " +
                    "FROM challenges " +
                    "WHERE (" +
                    $"   (host_user_id = @userId AND guest_user_id IN ({SubSqlValidUsers})) " +
                    $"   OR (guest_user_id = @userId AND host_user_id IN ({SubSqlValidUsers}))" +
                    ") AND is_accepted = 1 " +
                    "AND challenge_date > DATE(NOW())",
                    new { userId })
                .ConfigureAwait(false);
        }
    }
}
