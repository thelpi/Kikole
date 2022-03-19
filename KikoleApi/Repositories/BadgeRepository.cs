using System.Collections.Generic;
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

        public async Task<IReadOnlyCollection<BadgeDto>> GetBadgesAsync()
        {
            return await GetDtosAsync<BadgeDto>(
                    "badges")
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<UserBadgeDto>> GetUsersWithBadgeAsync(ulong badgeId)
        {
            return await GetDtosAsync<UserBadgeDto>(
                    "user_badges",
                    ("badge_id", badgeId))
                .ConfigureAwait(false);
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

            await ExecuteNonQueryAsync(
                    "UPDATE badges SET users = users + 1 WHERE id = @id",
                    new { id = userBadge.BadgeId })
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

            await ExecuteNonQueryAsync(
                    "UPDATE badges SET users = 0 WHERE id = @badgeId",
                    new { badgeId })
                .ConfigureAwait(false);
        }
    }
}
