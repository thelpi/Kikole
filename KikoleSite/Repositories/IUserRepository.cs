using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IUserRepository
{
    Task<ulong> CreateUserAsync(UserDto user);

    Task UpdateUserAsync(UserDto user);

    Task DeleteUserAsync(ulong userId);

    Task<UserDto?> GetUserByNormalizedLoginAsync(string normalizedLogin);

    Task<UserDto?> GetUserByIdAsync(ulong userId);

    Task<IReadOnlyCollection<UserDto>> GetUsersByIdsAsync(IReadOnlyCollection<ulong> userIds);

    Task<RegistrationGuidDto?> GetRegistrationGuidAsync(string id);

    Task LinkRegistrationGuidToUserAsync(string id, ulong userId);
}
