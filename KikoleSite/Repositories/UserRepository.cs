using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
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

    public async Task DisableUserAsync(ulong userId, string reason)
    {
        await ExecuteNonQueryAsync(
                "UPDATE users " +
                "SET is_disabled = 1, disabled_date = @disabledDate, disabled_reason = @reason " +
                "WHERE id = @userId AND is_disabled = 0",
                new { userId, reason, disabledDate = Clock.Now });
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
