using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories;

public class UserRepository : BaseRepository, IUserRepository
{
    public UserRepository(IConfiguration configuration, IClock clock)
        : base(configuration, clock)
    { }

    public async Task<int> GetUserCreationCountSinceAsync(string ip, DateTime since)
    {
        return await ExecuteScalarAsync(
                "SELECT COUNT(*) FROM users WHERE ip = @ip AND creation_date >= @since",
                new { ip, since },
                0);
    }

    public async Task<ulong> CreateUserAsync(UserDto user)
    {
        return await ExecuteInsertAsync(
                "users",
                ("login", user.Login),
                ("normalized_login", user.NormalizedLogin),
                ("password", user.Password),
                ("email_encrypted", user.EmailEncrypted),
                ("email_hash", user.EmailHash),
                ("email_confirmed", user.EmailConfirmed ? 1 : 0),
                ("language_id", user.LanguageId),
                ("user_type_id", user.UserTypeId),
                ("ip", user.Ip),
                ("is_disabled", user.IsDisabled ? 1 : 0),
                ("disabled_date", user.DisabledDate),
                ("disabled_reason", user.DisabledReason),
                ("concurrency_stamp", user.ConcurrencyStamp),
                ("security_stamp", user.SecurityStamp),
                ("lockout_end", user.LockoutEnd),
                ("access_failed_count", user.AccessFailedCount),
                ("lockout_enabled", user.LockoutEnabled ? 1 : 0),
                ("sponsor_user_id", user.SponsorUserId),
                ("creation_date", Clock.Now));
    }

    public async Task UpdateUserAsync(UserDto user)
    {
        await ExecuteNonQueryAsync(
                "UPDATE users " +
                "SET login = @login, " +
                "    normalized_login = @normalizedLogin, " +
                "    password = @password, " +
                "    email_encrypted = @emailEncrypted, " +
                "    email_hash = @emailHash, " +
                "    email_confirmed = @emailConfirmed, " +
                "    language_id = @languageId, " +
                "    user_type_id = @userTypeId, " +
                "    ip = @ip, " +
                "    concurrency_stamp = @concurrencyStamp, " +
                "    security_stamp = @securityStamp, " +
                "    lockout_end = @lockoutEnd, " +
                "    access_failed_count = @accessFailedCount, " +
                "    lockout_enabled = @lockoutEnabled " +
                "WHERE id = @id",
                new
                {
                    id = user.Id,
                    login = user.Login,
                    normalizedLogin = user.NormalizedLogin,
                    password = user.Password,
                    emailEncrypted = user.EmailEncrypted,
                    emailHash = user.EmailHash,
                    emailConfirmed = user.EmailConfirmed ? 1 : 0,
                    languageId = user.LanguageId,
                    userTypeId = user.UserTypeId,
                    ip = user.Ip,
                    concurrencyStamp = user.ConcurrencyStamp,
                    securityStamp = user.SecurityStamp,
                    lockoutEnd = user.LockoutEnd,
                    accessFailedCount = user.AccessFailedCount,
                    lockoutEnabled = user.LockoutEnabled ? 1 : 0
                });
    }

    public async Task DeleteUserAsync(ulong userId)
    {
        await ExecuteNonQueryAsync(
                "DELETE FROM users WHERE id = @userId",
                new { userId });
    }

    public async Task PurgeIpAddressesAsync(DateTime cutoff)
    {
        await ExecuteNonQueryAsync(
                "DELETE FROM login_history WHERE creation_date < @cutoff",
                new { cutoff });

        await ExecuteNonQueryAsync(
                "UPDATE users SET ip = NULL WHERE creation_date < @cutoff AND ip IS NOT NULL",
                new { cutoff });
    }

    public async Task<bool> DeleteUserWithAllDataAsync(ulong userId)
    {
        var adminType = (ulong)UserTypes.Administrator;

        // contrôles hors transaction : rien n'est ouvert tant que les conditions ne sont pas réunies
        var userType = await ExecuteScalarAsync<ulong?>(
            "SELECT user_type_id FROM users WHERE id = @userId",
            new { userId });

        if (userType == null || userType == adminType)
            return false;

        var adminId = await ExecuteScalarAsync<ulong?>(
                "SELECT id FROM users WHERE user_type_id = @adminType ORDER BY id LIMIT 1",
                new { adminType })
            ?? throw new InvalidOperationException("Aucun administrateur pour reprendre les kikolés du compte supprimé.");

        try
        {
            return await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                var parameters = new { userId, adminId, adminType };

                // kikolés publiés ou validés : conservés, repris par l'administrateur ; les autres
                // (en attente, refusés) sont supprimés avec leurs clubs et leurs indices
                var statements = new[]
                {
                    "UPDATE players SET creation_user_id = @adminId " +
                        "WHERE creation_user_id = @userId AND (publication_date IS NOT NULL OR acceptance_date IS NOT NULL)",
                    "DELETE FROM player_clue_translations WHERE player_id IN (SELECT id FROM players WHERE creation_user_id = @userId)",
                    "DELETE FROM player_clubs WHERE player_id IN (SELECT id FROM players WHERE creation_user_id = @userId)",
                    "DELETE FROM players WHERE creation_user_id = @userId",
                    "DELETE FROM leaders WHERE user_id = @userId",
                    "DELETE FROM proposals WHERE user_id = @userId",
                    "DELETE FROM user_badges WHERE user_id = @userId",
                    "DELETE FROM login_history WHERE user_id = @userId",
                    "DELETE FROM registration_guids WHERE user_id = @userId",
                    "DELETE FROM discussion_messages WHERE discussion_id IN (SELECT id FROM discussions WHERE user_id = @userId)",
                    "DELETE FROM discussions WHERE user_id = @userId",
                    "UPDATE users SET sponsor_user_id = NULL WHERE sponsor_user_id = @userId"
                };

                foreach (var statement in statements)
                    await connection.ExecuteAsync(statement, parameters, transaction);

                // garde finale, atomique avec le reste : si le compte a disparu entre-temps (double
                // envoi) ou est devenu administrateur, tout est annulé
                var deleted = await connection.ExecuteAsync(
                    "DELETE FROM users WHERE id = @userId AND user_type_id <> @adminType",
                    parameters,
                    transaction);

                if (deleted == 0)
                    throw new AccountNotDeletableException();

                return true;
            });
        }
        catch (AccountNotDeletableException)
        {
            return false;
        }
    }

    private sealed class AccountNotDeletableException : Exception;
    public async Task DisableUserAsync(ulong userId, string reason)
    {
        await ExecuteNonQueryAsync(
                "UPDATE users " +
                "SET is_disabled = 1, disabled_date = @disabledDate, disabled_reason = @reason " +
                "WHERE id = @userId AND is_disabled = 0",
                new { userId, reason, disabledDate = Clock.Now });
    }

    public async Task<(IReadOnlyList<UserDto> Users, int Total)> SearchUsersAsync(
        string? login, UserStatusFilter status, UserTypes? type, bool descending, int page, int pageSize)
    {
        var conditions = new List<string> { "user_type_id != @adminType" };

        if (!string.IsNullOrWhiteSpace(login))
            conditions.Add("login LIKE @pattern ESCAPE '\\\\'");

        if (status == UserStatusFilter.Enabled)
            conditions.Add("is_disabled = 0");
        else if (status == UserStatusFilter.Disabled)
            conditions.Add("is_disabled = 1");

        if (type.HasValue)
            conditions.Add("user_type_id = @type");

        var where = "WHERE " + string.Join(" AND ", conditions);
        var direction = descending ? "DESC" : "ASC";
        var parameters = new
        {
            adminType = (ulong)UserTypes.Administrator,
            pattern = LikePattern(login),
            type = (ulong?)type,
            limit = pageSize,
            offset = (page - 1) * pageSize
        };

        var total = await ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM users {where}", parameters);

        var users = await ExecuteReaderAsync<UserDto>(
                $"SELECT * FROM users {where} " +
                $"ORDER BY creation_date {direction}, id {direction} " +
                "LIMIT @limit OFFSET @offset",
                parameters);

        return (users, total);
    }

    public async Task<IReadOnlyList<string>> SearchLoginsAsync(string term, int max)
    {
        return await ExecuteReaderAsync<string>(
                "SELECT login FROM users " +
                "WHERE user_type_id != @adminType AND login LIKE @pattern ESCAPE '\\\\' " +
                "ORDER BY login LIMIT @max",
                new
                {
                    adminType = (ulong)UserTypes.Administrator,
                    pattern = LikePattern(term),
                    max
                });
    }

    public async Task ChangeUserTypeAsync(ulong userId, UserTypes type)
    {
        if (type is not (UserTypes.StandardUser or UserTypes.PowerUser))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Seuls les paliers standard et avancé sont attribuables.");

        await ExecuteNonQueryAsync(
                "UPDATE users SET user_type_id = @type " +
                "WHERE id = @userId AND is_disabled = 0 " +
                "AND user_type_id IN (@standardType, @powerType)",
                new
                {
                    userId,
                    type = (ulong)type,
                    standardType = (ulong)UserTypes.StandardUser,
                    powerType = (ulong)UserTypes.PowerUser
                });
    }

    private static string? LikePattern(string? term)
    {
        return string.IsNullOrWhiteSpace(term)
            ? null
            : "%" + term.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    }

    public async Task<UserDto?> GetUserByNormalizedLoginAsync(string normalizedLogin)
    {
        return await GetDtoAsync<UserDto>(
                "users",
                ("normalized_login", normalizedLogin),
                ("is_disabled", 0));
    }

    public async Task<UserDto?> GetUserByEmailHashAsync(string emailHash)
    {
        return await GetDtoAsync<UserDto>(
                "users",
                ("email_hash", emailHash),
                ("is_disabled", 0));
    }

    public async Task<UserDto?> GetUserByEmailHashIncludingDisabledAsync(string emailHash)
    {
        return await GetDtoAsync<UserDto>("users", ("email_hash", emailHash));
    }

    public async Task<UserDto?> GetUserByIdAsync(ulong userId)
    {
        return await GetDtoAsync<UserDto>("users",
                ("id", userId),
                ("is_disabled", 0));
    }

    public async Task<IReadOnlyCollection<UserDto>> GetUsersByIdsAsync(IReadOnlyCollection<ulong> userIds)
    {
        if (userIds.Count == 0)
            return [];

        return await ExecuteReaderAsync<UserDto>(
                "SELECT * FROM users WHERE id IN @userIds AND is_disabled = 0",
                new { userIds });
    }

    public async Task<IReadOnlyCollection<UserDto>> GetUsersByIdsIncludingDisabledAsync(IReadOnlyCollection<ulong> userIds)
    {
        if (userIds.Count == 0)
            return [];

        return await ExecuteReaderAsync<UserDto>(
                "SELECT * FROM users WHERE id IN @userIds",
                new { userIds });
    }

    public async Task<UserDto?> GetUserByIdIncludingDisabledAsync(ulong userId)
    {
        return await GetDtoAsync<UserDto>("users", ("id", userId));
    }

    public async Task<IReadOnlyCollection<UserDto>> GetGodchildrenAsync(ulong sponsorUserId)
    {
        return await GetDtosAsync<UserDto>("users", ("sponsor_user_id", sponsorUserId));
    }

    public async Task<IReadOnlyCollection<ulong>> GetSponsorUserIdsAsync()
    {
        return await ExecuteReaderAsync<ulong>(
                "SELECT DISTINCT sponsor_user_id FROM users WHERE sponsor_user_id IS NOT NULL",
                null);
    }

    public async Task<RegistrationGuidDto?> GetRegistrationGuidAsync(string id)
    {
        return await GetDtoAsync<RegistrationGuidDto>(
                "registration_guids",
                ("id", id));
    }

    public async Task LinkRegistrationGuidToUserAsync(string id, ulong userId)
    {
        await ExecuteNonQueryAsync(
                "UPDATE registration_guids " +
                "SET user_id = @userId " +
                "WHERE id = @id",
                new
                {
                    id,
                    userId
                });
    }

    public async Task CreateLoginHistoryAsync(ulong userId, string? ip)
    {
        await ExecuteInsertAsync(
                "login_history",
                ("user_id", userId),
                ("ip", ip),
                ("creation_date", Clock.Now));
    }
}
