using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
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
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task<UserDto> GetUserByLoginPasswordAsync(string login, string password)
        {
            return await GetDtoAsync<UserDto>(
                    "users",
                    ("login", login),
                    ("password", password))
                .ConfigureAwait(false);
        }

        public async Task<UserDto> GetUserByLoginAsync(string login)
        {
            return await GetDtoAsync<UserDto>(
                    "users",
                    ("login", login))
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

        private async Task<bool> ResetUserPasswordAsync(string login, (string, string) fieldInfo, string newPassword)
        {
            var user = await GetDtoAsync<UserDto>(
                       "users",
                       ("login", login),
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
