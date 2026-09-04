using System;

namespace KikoleSite;

/// <summary>
/// Bornes calendaires du jeu.
/// </summary>
/// <remarks>
/// Fournisseur transverse, au même titre que <see cref="IClock"/> : il est consommé par
/// les contrôleurs comme par les services, et ne dépend lui-même de rien. Ses valeurs
/// sont déduites des données au démarrage par <see cref="GameCalendarLoader"/>.
/// </remarks>
public interface IGameCalendar
{
    /// <summary>
    /// La journée cachée : le tout premier joueur publié, jouable une fois débloqué.
    /// </summary>
    /// <remarks>
    /// Elle n'est pas proposée le jour même, mais se débloque quand le joueur a trouvé
    /// ou créé toutes les journées depuis <see cref="FirstDate"/>.
    /// </remarks>
    DateTime HiddenDate { get; }

    /// <summary>
    /// La première journée jouable, soit le lendemain de <see cref="HiddenDate"/>.
    /// </summary>
    DateTime FirstDate { get; }

    /// <summary>
    /// Le premier jour du mois de <see cref="FirstDate"/>, origine du palmarès mensuel.
    /// </summary>
    DateTime FirstMonth { get; }
}
