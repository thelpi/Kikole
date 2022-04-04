using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    public class BadgeRepository : BaseRepository, IBadgeRepository
    {
        public BadgeRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<IReadOnlyCollection<BadgeDto>> GetBadgesAsync(bool includeHidden)
        {
            var parameters = new List<(string, object)>();
            if (!includeHidden)
                parameters.Add(("hidden", 0));

            return await GetDtosAsync<BadgeDto>(
                    "badges",
                    parameters.ToArray())
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<UserBadgeDto>> GetUsersWithBadgeAsync(ulong badgeId)
        {
            return await ExecuteReaderAsync<UserBadgeDto>(
                    "SELECT * FROM user_badges " +
                    "WHERE badge_id = @badgeId " +
                    $"AND user_id IN ({SubSqlValidUsers})",
                    new { badgeId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<UserBadgeDto>> GetUsersOfTheDayWithBadgeAsync(ulong badgeId, DateTime date)
        {
            var userBadges = await GetUsersWithBadgeAsync(badgeId)
                .ConfigureAwait(false);

            return userBadges.Where(_ => _.GetDate == date.Date).ToList();
        }

        public async Task<bool> CheckUserHasBadgeAsync(ulong userId, ulong badgeId)
        {
            var data = await GetDtoAsync<UserBadgeDto>(
                    "user_badges",
                    ("user_id", userId),
                    ("badge_id", badgeId))
                .ConfigureAwait(false);

            return data != null;
        }

        public async Task InsertUserBadgeAsync(UserBadgeDto userBadge)
        {
            await ExecuteInsertAsync(
                    "user_badges",
                    ("badge_id", userBadge.BadgeId),
                    ("get_date", userBadge.GetDate.Date),
                    ("user_id", userBadge.UserId))
                .ConfigureAwait(false);
        }

        public async Task RemoveUserBadgeAsync(UserBadgeDto userBadge)
        {
            await ExecuteNonQueryAsync(
                    "DELETE FROM user_badges WHERE badge_id = @BadgeId AND user_id = @UserId",
                    new { userBadge.BadgeId, userBadge.UserId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<UserBadgeDto>> GetUserBadgesAsync(ulong userId)
        {
            return await GetDtosAsync<UserBadgeDto>(
                    "user_badges",
                    ("user_id", userId))
                .ConfigureAwait(false);
        }

        public async Task ResetBadgeDatasAsync(ulong badgeId)
        {
            await ExecuteNonQueryAsync(
                    "DELETE FROM user_badges WHERE badge_id = @badgeId",
                    new { badgeId })
                .ConfigureAwait(false);
        }

        public async Task<string> GetBadgeDescriptionAsync(ulong badgeId, ulong languageId)
        {
            return await ExecuteScalarAsync<string>(
                    "SELECT description FROM badge_translations " +
                    "WHERE badge_id = @badgeId " +
                    "AND language_id = @languageId",
                    new { badgeId, languageId })
                .ConfigureAwait(false);
        }
    }
}
