using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IBadgeRepository
{
    /// <summary>Ne renvoie jamais les badges desactives (<see cref="BadgeDto.IsDisabled"/>),
    /// quel que soit <paramref name="includeHidden"/> : un badge desactive est virtuellement
    /// supprime, un badge cache reste visible selon le contexte (proprietaire/admin).</summary>
    Task<IReadOnlyCollection<BadgeDto>> GetBadgesAsync(bool includeHidden);

    Task InsertUserBadgeAsync(UserBadgeDto userBadge);

    Task RemoveUserBadgeAsync(UserBadgeDto userBadge);

    Task<IReadOnlyCollection<UserBadgeDto>> GetUsersWithBadgeAsync(ulong badgeId);

    Task<IReadOnlyCollection<UserBadgeDto>> GetUsersOfTheDayWithBadgeAsync(ulong badgeId, DateOnly date);

    Task<bool> CheckUserHasBadgeAsync(ulong userId, ulong badgeId);

    Task<IReadOnlyCollection<UserBadgeDto>> GetUserBadgesAsync(ulong userId);

    /// <summary>Tous les badges de tous les utilisateurs, sans filtre (ni sur l'utilisateur,
    /// ni sur le badge) : utilise par le classement "% de badges obtenus"
    /// (<see cref="Services.LeaderService"/>), qui filtre ensuite lui-meme sur sa propre
    /// population et sur les badges actifs.</summary>
    Task<IReadOnlyCollection<UserBadgeDto>> GetAllUserBadgesAsync();

    Task ResetBadgeDatasAsync(ulong badgeId);

    Task<BadgeTranslationDto?> GetBadgeTranslationAsync(ulong badgeId, ulong languageId);
}
