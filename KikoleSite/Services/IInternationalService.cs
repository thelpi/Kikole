using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;

namespace KikoleSite.Services;

/// <summary>
/// Référentiels partagés — clubs, nationalités, continents — et leur mise en cache.
/// </summary>
/// <remarks>
/// Ces données ne changent qu'à l'initiative d'un administrateur : elles sont chargées
/// une fois puis conservées jusqu'à invalidation explicite.
/// </remarks>
public interface IInternationalService
{
    /// <summary>
    /// Tous les clubs, triés par nom.
    /// </summary>
    /// <returns>Les clubs du référentiel.</returns>
    Task<IReadOnlyCollection<Club>> GetClubsAsync();

    /// <summary>
    /// Un club par son identifiant.
    /// </summary>
    /// <param name="clubId">Identifiant du club.</param>
    /// <returns>Le club, ou <c>null</c> s'il n'existe pas.</returns>
    Task<Club?> GetClubAsync(ulong clubId);

    /// <summary>
    /// Crée le club s'il n'a pas d'identifiant, le met à jour sinon, et rafraîchit le cache.
    /// </summary>
    /// <remarks>
    /// Toute écriture sur le référentiel passe par ici : c'est ce qui garantit que le cache
    /// ne peut pas devenir obsolète par oubli d'invalidation.
    /// </remarks>
    /// <param name="request">Club à enregistrer, préalablement validé.</param>
    /// <returns>Rien.</returns>
    Task CreateOrUpdateClubAsync(ClubRequest request);

    /// <summary>
    /// Les nationalités dans la langue demandée, indexées par leur code pays.
    /// </summary>
    /// <param name="language">Langue d'affichage.</param>
    /// <returns>Code pays vers libellé.</returns>
    Task<IReadOnlyDictionary<ulong, string>> GetCountriesAsync(Languages language);

    /// <summary>
    /// Les continents dans la langue demandée, indexés par leur identifiant.
    /// </summary>
    /// <param name="language">Langue d'affichage.</param>
    /// <returns>Identifiant de continent vers libellé.</returns>
    Task<IReadOnlyDictionary<ulong, string>> GetContinentsAsync(Languages language);
}
