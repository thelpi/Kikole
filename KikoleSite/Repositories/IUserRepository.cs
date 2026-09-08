using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IUserRepository
{
    Task<ulong> CreateUserAsync(UserDto user);

    /// <summary>Nombre de comptes créés depuis <paramref name="ip"/> depuis <paramref name="since"/> (lutte anti-multi-compte).</summary>
    Task<int> GetUserCreationCountSinceAsync(string ip, DateTime since);

    Task UpdateUserAsync(UserDto user);

    Task DeleteUserAsync(ulong userId);

    Task<UserDto?> GetUserByNormalizedLoginAsync(string normalizedLogin);

    Task<UserDto?> GetUserByIdAsync(ulong userId);

    Task<IReadOnlyCollection<UserDto>> GetUsersByIdsAsync(IReadOnlyCollection<ulong> userIds);

    /// <summary>
    /// Utilisateur par identifiant, y compris s'il est désactivé (contrairement à
    /// <see cref="GetUserByIdAsync"/>) : sert à afficher le parrain d'un compte même si
    /// celui-ci a depuis été désactivé.
    /// </summary>
    Task<UserDto?> GetUserByIdIncludingDisabledAsync(ulong userId);

    /// <summary>Filleuls (utilisateurs parrainés par <paramref name="sponsorUserId"/>), y compris les désactivés.</summary>
    Task<IReadOnlyCollection<UserDto>> GetGodchildrenAsync(ulong sponsorUserId);

    Task<RegistrationGuidDto?> GetRegistrationGuidAsync(string id);

    Task LinkRegistrationGuidToUserAsync(string id, ulong userId);

    /// <summary>Historise une connexion réussie (lutte anti-multi-compte).</summary>
    Task CreateLoginHistoryAsync(ulong userId, string? ip);
}
