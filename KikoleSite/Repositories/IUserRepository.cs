using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Repositories;

public interface IUserRepository
{
    Task<ulong> CreateUserAsync(UserDto user);

    /// <summary>Nombre de comptes créés depuis <paramref name="ip"/> depuis <paramref name="since"/> (lutte anti-multi-compte).</summary>
    Task<int> GetUserCreationCountSinceAsync(string ip, DateTime since);

    Task UpdateUserAsync(UserDto user);

    Task DeleteUserAsync(ulong userId);

    /// <summary>Désactive un compte en gardant la date et la raison. Il n'existe volontairement pas d'opération inverse.</summary>
    Task DisableUserAsync(ulong userId, string reason);

    /// <summary>
    /// Page d'utilisateurs pour l'administration (désactivés inclus, administrateurs exclus),
    /// triés par date de création, avec le total des résultats avant pagination.
    /// <paramref name="login"/> est cherché par « contient ».
    /// </summary>
    Task<(IReadOnlyList<UserDto> Users, int Total)> SearchUsersAsync(
        string? login, UserStatusFilter status, UserTypes? type, bool descending, int page, int pageSize);

    /// <summary>Logins contenant <paramref name="term"/> (désactivés inclus, administrateurs exclus), limités à <paramref name="max"/>.</summary>
    Task<IReadOnlyList<string>> SearchLoginsAsync(string term, int max);

    /// <summary>Passe un compte actif entre palier standard et palier avancé ; sans effet sur un administrateur ou un compte désactivé.</summary>
    Task ChangeUserTypeAsync(ulong userId, UserTypes type);

    Task<UserDto?> GetUserByNormalizedLoginAsync(string normalizedLogin);

    /// <summary>Utilisateur par empreinte d'email (compte actif uniquement), pour la connexion par email.</summary>
    Task<UserDto?> GetUserByEmailHashAsync(string emailHash);

    /// <summary>
    /// Utilisateur par empreinte d'email, y compris s'il est desactive (contrairement a
    /// <see cref="GetUserByEmailHashAsync"/>) : sert au controle d'unicite, qui doit
    /// couvrir les comptes desactives.
    /// </summary>
    Task<UserDto?> GetUserByEmailHashIncludingDisabledAsync(string emailHash);

    Task<UserDto?> GetUserByIdAsync(ulong userId);

    Task<IReadOnlyCollection<UserDto>> GetUsersByIdsAsync(IReadOnlyCollection<ulong> userIds);

    /// <summary>Comme <see cref="GetUsersByIdsAsync"/>, mais les comptes désactivés sont renvoyés aussi.</summary>
    Task<IReadOnlyCollection<UserDto>> GetUsersByIdsIncludingDisabledAsync(IReadOnlyCollection<ulong> userIds);

    /// <summary>
    /// Utilisateur par identifiant, y compris s'il est désactivé (contrairement à
    /// <see cref="GetUserByIdAsync"/>) : sert à afficher le parrain d'un compte même si
    /// celui-ci a depuis été désactivé.
    /// </summary>
    Task<UserDto?> GetUserByIdIncludingDisabledAsync(ulong userId);

    /// <summary>Filleuls (utilisateurs parrainés par <paramref name="sponsorUserId"/>), y compris les désactivés.</summary>
    Task<IReadOnlyCollection<UserDto>> GetGodchildrenAsync(ulong sponsorUserId);

    /// <summary>Identifiants distincts des utilisateurs ayant au moins un filleul (sert au recalcul des badges de parrainage).</summary>
    Task<IReadOnlyCollection<ulong>> GetSponsorUserIdsAsync();

    Task<RegistrationGuidDto?> GetRegistrationGuidAsync(string id);

    Task LinkRegistrationGuidToUserAsync(string id, ulong userId);

    /// <summary>Historise une connexion réussie (lutte anti-multi-compte).</summary>
    Task CreateLoginHistoryAsync(ulong userId, string? ip);
}
