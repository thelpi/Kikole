using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Interfaces.Repositories
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

        Task<string> GetBadgeDescriptionAsync(ulong badgeId, ulong languageId);
    }
}
