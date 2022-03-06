using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IUserRepository
    {
        Task<ulong> CreateUserAsync(UserDto user);

        Task<UserDto> GetUserByLoginPasswordAsync(string login, string password);

        Task<UserDto> GetUserByLoginAsync(string login);

        Task<bool> ResetUserKnownPasswordAsync(string login, string oldPassword, string newPassword);

        Task<bool> ResetUserUnknownPasswordAsync(string login, string passwordResetAnswer, string newPassword);
    }
}
