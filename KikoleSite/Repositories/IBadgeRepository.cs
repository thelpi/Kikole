using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IBadgeRepository
{
    Task<IReadOnlyCollection<BadgeDto>> GetBadgesAsync(bool includeHidden);

    Task InsertUserBadgeAsync(UserBadgeDto userBadge);

    Task RemoveUserBadgeAsync(UserBadgeDto userBadge);

    Task<IReadOnlyCollection<UserBadgeDto>> GetUsersWithBadgeAsync(ulong badgeId);

    Task<IReadOnlyCollection<UserBadgeDto>> GetUsersOfTheDayWithBadgeAsync(ulong badgeId, DateOnly date);

    Task<bool> CheckUserHasBadgeAsync(ulong userId, ulong badgeId);

    Task<IReadOnlyCollection<UserBadgeDto>> GetUserBadgesAsync(ulong userId);

    Task ResetBadgeDatasAsync(ulong badgeId);

    Task<BadgeTranslationDto?> GetBadgeTranslationAsync(ulong badgeId, ulong languageId);
}
