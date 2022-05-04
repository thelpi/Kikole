using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Api.Repositories
{
    [ExcludeFromCodeCoverage]
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
                    ("password", user.Password),
                    ("password_reset_question", user.PasswordResetQuestion),
                    ("password_reset_answer", user.PasswordResetAnswer),
                    ("language_id", user.LanguageId),
                    ("user_type_id", user.UserTypeId),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task<UserDto> GetUserByLoginPasswordAsync(string login, string password)
        {
            return await GetDtoAsync<UserDto>(
                    "users",
                    ("login", login),
                    ("password", password),
                    ("is_disabled", 0))
                .ConfigureAwait(false);
        }

        public async Task<UserDto> GetUserByLoginAsync(string login)
        {
            return await GetDtoAsync<UserDto>(
                    "users",
                    ("login", login),
                    ("is_disabled", 0))
                .ConfigureAwait(false);
        }

        public async Task<bool> ResetUserKnownPasswordAsync(string login, string oldPassword, string newPassword)
        {
            return await ResetUserPasswordAsync(
                    login,
                    ("password", oldPassword),
                    newPassword)
                .ConfigureAwait(false);
        }

        public async Task<bool> ResetUserUnknownPasswordAsync(string login, string passwordResetAnswer, string newPassword)
        {
            return await ResetUserPasswordAsync(
                    login,
                    ("password_reset_answer", passwordResetAnswer),
                    newPassword)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<UserDto>> GetActiveUsersAsync()
        {
            // TODO: could use "base.SubSqlValidUsers" maybe?
            return await ExecuteReaderAsync<UserDto>(
                    "SELECT * FROM users " +
                    "WHERE is_disabled = 0 AND user_type_id != @adminId",
                    new { adminId = (ulong)UserTypes.Administrator })
                .ConfigureAwait(false);
        }

        public async Task<UserDto> GetUserByIdAsync(ulong userId)
        {
            return await GetDtoAsync<UserDto>("users",
                    ("id", userId),
                    ("is_disabled", 0))
                .ConfigureAwait(false);
        }

        public async Task ResetUserQAndAAsync(ulong userId, string question, string anwser)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE users " +
                    "SET password_reset_question = @question, password_reset_answer = @anwser " +
                    "WHERE id = @userId",
                    new
                    {
                        userId,
                        question,
                        anwser
                    })
                .ConfigureAwait(false);
        }

        private async Task<bool> ResetUserPasswordAsync(string login, (string, string) fieldInfo, string newPassword)
        {
            var user = await GetDtoAsync<UserDto>(
                    "users",
                    ("login", login),
                    ("is_disabled", 0),
                    (fieldInfo.Item1, fieldInfo.Item2))
                .ConfigureAwait(false);

            if (user == null)
                return false;

            await ExecuteNonQueryAsync(
                    "UPDATE users SET password = @password WHERE id = @id",
                    new
                    {
                        id = user.Id,
                        password = newPassword
                    })
                .ConfigureAwait(false);

            return true;
        }
    }
}
