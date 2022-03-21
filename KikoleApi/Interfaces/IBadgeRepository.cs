using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IBadgeRepository
    {
        Task<IReadOnlyCollection<BadgeDto>> GetBadgesAsync(bool includeHidden);

        Task InsertUserBadgeAsync(UserBadgeDto userBadge);

        Task RemoveUserBadgeAsync(UserBadgeDto userBadge);

        Task<IReadOnlyCollection<UserBadgeDto>> GetUsersWithBadgeAsync(ulong badgeId);

        Task<IReadOnlyCollection<UserBadgeDto>> GetUsersOfTheDayWithBadgeAsync(ulong badgeId, DateTime date);

        Task<bool> CheckUserHasBadgeAsync(ulong userId, ulong badgeId);

        Task<IReadOnlyCollection<UserBadgeDto>> GetUserBadgesAsync(ulong userId);

        Task ResetBadgeDatasAsync(ulong badgeId);
    }
}
