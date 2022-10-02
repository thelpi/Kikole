using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<ulong> CreateUserAsync(UserDto user);

        Task<UserDto> GetUserByLoginAsync(string login);

        Task<bool> ResetUserKnownPasswordAsync(string login, string oldPassword, string newPassword);

        Task<bool> ResetUserUnknownPasswordAsync(string login, string passwordResetAnswer, string newPassword);

        Task<UserDto> GetUserByIdAsync(ulong userId);

        Task ResetUserQAndAAsync(ulong userId, string question, string anwser);

        Task<RegistrationGuidDto> GetRegistrationGuidAsync(string id);

        Task LinkRegistrationGuidToUserAsync(string id, ulong userId);
    }
}
