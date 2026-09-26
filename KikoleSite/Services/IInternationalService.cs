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
    /// Indique si un autre club du même pays porte déjà l'un des noms de la demande
    /// (nom principal ou alternatif, dans n'importe quelle langue, sans tenir compte de la
    /// casse, des accents ni de la ponctuation : "Milan A.C." = "Milan AC").
    /// </summary>
    /// <remarks>
    /// La règle est vraie sur les données actuelles (aucun doublon de nom dans un même pays,
    /// vérifié en base le 2026-09-26, y compris ponctuation ignorée) ; elle sert à ne pas la casser.
    /// </remarks>
    /// <param name="request">Club à enregistrer ; son identifiant est exclu de la comparaison.</param>
    /// <returns><c>true</c> si un autre club du pays porte déjà l'un de ces noms.</returns>
    Task<bool> ClubNameAlreadyExistsAsync(ClubRequest request);

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

    /// <summary>
    /// La confédération (continent) de chaque pays, indépendante de la langue. Sert à
    /// déduire le continent d'un joueur depuis son pays plutôt que de le stocker.
    /// </summary>
    /// <returns>Identifiant de pays vers identifiant de continent.</returns>
    Task<IReadOnlyDictionary<ulong, ulong>> GetCountryContinentsAsync();
}
