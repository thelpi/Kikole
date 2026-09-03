using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models;
using KikoleSite.Models.Enums;

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
    /// Oublie les clubs en cache. À appeler après toute écriture sur le référentiel.
    /// </summary>
    void InvalidateClubs();
}
