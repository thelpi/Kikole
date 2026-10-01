using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories;

public class BadgeRepository : BaseRepository, IBadgeRepository
{
    public BadgeRepository(IConfiguration configuration, IClock clock)
        : base(configuration, clock)
    { }

    public async Task<IReadOnlyCollection<BadgeDto>> GetBadgesAsync(bool includeHidden)
    {
        // is_disabled exclu sans condition : contrairement a "hidden", il n'y a aucun
        // contexte ou un badge desactive doit ressortir (suppression virtuelle).
        var parameters = new List<(string, object?)> { ("is_disabled", 0) };
        if (!includeHidden)
            parameters.Add(("hidden", 0));

        return await GetDtosAsync<BadgeDto>(
                "badges",
                parameters.ToArray());
    }

    public async Task<IReadOnlyCollection<UserBadgeDto>> GetUsersWithBadgeAsync(ulong badgeId)
    {
        return await ExecuteReaderAsync<UserBadgeDto>(
                "SELECT * FROM user_badges " +
                "WHERE badge_id = @badgeId " +
                $"AND user_id IN ({SubSqlValidUsers})",
                new { badgeId });
    }

    public async Task<IReadOnlyCollection<UserBadgeDto>> GetUsersOfTheDayWithBadgeAsync(ulong badgeId, DateOnly date)
    {
        return await ExecuteReaderAsync<UserBadgeDto>(
                "SELECT * FROM user_badges " +
                "WHERE badge_id = @badgeId " +
                "AND get_date = @date " +
                $"AND user_id IN ({SubSqlValidUsers})",
                new { badgeId, date });
    }

    public async Task<bool> CheckUserHasBadgeAsync(ulong userId, ulong badgeId)
    {
        var data = await GetDtoAsync<UserBadgeDto>(
                "user_badges",
                ("user_id", userId),
                ("badge_id", badgeId));

        return data != null;
    }

    public async Task InsertUserBadgeAsync(UserBadgeDto userBadge)
    {
        await ExecuteInsertAsync(
                "user_badges",
                ("badge_id", userBadge.BadgeId),
                ("get_date", userBadge.GetDate),
                ("user_id", userBadge.UserId));
    }

    public async Task RemoveUserBadgeAsync(UserBadgeDto userBadge)
    {
        await ExecuteNonQueryAsync(
                "DELETE FROM user_badges WHERE badge_id = @BadgeId AND user_id = @UserId",
                new { userBadge.BadgeId, userBadge.UserId });
    }

    public async Task<IReadOnlyCollection<UserBadgeDto>> GetUserBadgesAsync(ulong userId)
    {
        return await GetDtosAsync<UserBadgeDto>(
                "user_badges",
                ("user_id", userId));
    }

    public async Task ResetBadgeDatasAsync(ulong badgeId)
    {
        await ExecuteNonQueryAsync(
                "DELETE FROM user_badges WHERE badge_id = @badgeId",
                new { badgeId });
    }

    public async Task<BadgeTranslationDto?> GetBadgeTranslationAsync(ulong badgeId, ulong languageId)
    {
        return await ExecuteScalarAsync<BadgeTranslationDto>(
                "SELECT name, description FROM badge_translations " +
                "WHERE badge_id = @badgeId " +
                "AND language_id = @languageId",
                new { badgeId, languageId });
    }
}
