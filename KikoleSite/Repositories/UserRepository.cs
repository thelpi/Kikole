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

    public async Task<ulong> CreateUserAsync(UserDto user)
    {
        return await ExecuteInsertAsync(
                "users",
                ("login", user.Login),
                ("normalized_login", user.NormalizedLogin),
                ("password", user.Password),
                ("password_reset_question", user.PasswordResetQuestion),
                ("password_reset_answer", user.PasswordResetAnswer),
                ("language_id", user.LanguageId),
                ("user_type_id", user.UserTypeId),
                ("ip", user.Ip),
                ("is_disabled", user.IsDisabled ? 1 : 0),
                ("concurrency_stamp", user.ConcurrencyStamp),
                ("security_stamp", user.SecurityStamp),
                ("lockout_end", user.LockoutEnd),
                ("access_failed_count", user.AccessFailedCount),
                ("lockout_enabled", user.LockoutEnabled ? 1 : 0),
                ("creation_date", Clock.Now));
    }

    public async Task UpdateUserAsync(UserDto user)
    {
        await ExecuteNonQueryAsync(
                "UPDATE users " +
                "SET login = @login, " +
                "    normalized_login = @normalizedLogin, " +
                "    password = @password, " +
                "    password_reset_question = @passwordResetQuestion, " +
                "    password_reset_answer = @passwordResetAnswer, " +
                "    language_id = @languageId, " +
                "    user_type_id = @userTypeId, " +
                "    ip = @ip, " +
                "    is_disabled = @isDisabled, " +
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
                    passwordResetQuestion = user.PasswordResetQuestion,
                    passwordResetAnswer = user.PasswordResetAnswer,
                    languageId = user.LanguageId,
                    userTypeId = user.UserTypeId,
                    ip = user.Ip,
                    isDisabled = user.IsDisabled ? 1 : 0,
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

    public async Task<UserDto?> GetUserByNormalizedLoginAsync(string normalizedLogin)
    {
        return await GetDtoAsync<UserDto>(
                "users",
                ("normalized_login", normalizedLogin),
                ("is_disabled", 0));
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
}
