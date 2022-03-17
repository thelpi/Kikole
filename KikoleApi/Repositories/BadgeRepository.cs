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

        public async Task<IReadOnlyCollection<UserBadgeDto>> GetUsersWithBadge(ulong badgeId)
        {
            return await GetDtosAsync<UserBadgeDto>(
                    "user_badges",
                    ("badge_id", badgeId))
                .ConfigureAwait(false);
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
    }
}
