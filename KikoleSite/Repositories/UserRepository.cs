using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories
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
                    ("user_type_id", user.UserTypeId),
                    ("ip", user.Ip),
                    ("creation_date", Clock.Now))
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

        public async Task<RegistrationGuidDto> GetRegistrationGuidAsync(string id)
        {
            return await GetDtoAsync<RegistrationGuidDto>(
                    "registration_guids",
                    ("id", id))
                .ConfigureAwait(false);
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
