using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
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
                    ("challenge_date", challenge.ChallengeDate.Date),
                    ("guest_user_id", challenge.GuestUserId),
                    ("host_user_id", challenge.HostUserId),
                    ("points_rate", challenge.PointsRate),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task RespondToChallengeAsync(ulong challengeId, bool accept)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE challenges SET is_accepted = @isAccepted WHERE id = @challengeId",
                    new { challengeId, isAccepted = accept ? 1 : 0 })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetAcceptedChallengesOfTheDayAsync(DateTime date)
        {
            return await GetDtosAsync<ChallengeDto>("challenges",
                    ("is_accepted", 1),
                    ("challenge_date", date.Date))
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetPendingChallengesByGuestUserAsync(ulong userId)
        {
            return await ExecuteReaderAsync<ChallengeDto>(
                    "SELECT * FROM challenges " +
                    "WHERE guest_user_id = @userId " +
                    $"AND is_accepted IS NULL",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ChallengeDto>> GetChallengesByUserAndByDateAsync(ulong userId, DateTime date)
        {
            return await ExecuteReaderAsync<ChallengeDto>(
                    "SELECT * FROM challenges " +
                    "WHERE challenge_date = @cDate " +
                    "AND (guest_user_id = @userId or host_user_id = @userId)",
                    new { cDate = date.Date, userId })
                .ConfigureAwait(false);
        }

        public async Task<ChallengeDto> GetChallengeByIdAsync(ulong id)
        {
            return await GetDtoAsync<ChallengeDto>(
                    "challenges",
                    ("id", id))
                .ConfigureAwait(false);
        }
    }
}
