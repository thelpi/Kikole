using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Helpers;

/// <summary>
/// Pages publiques destinées aux moteurs de recherche. Tout le reste (compte, administration,
/// pages d'un joueur, navigation par jour...) est exclu de l'indexation par défaut.
/// </summary>
public static class SeoPages
{
    public static readonly IReadOnlyList<string> IndexablePaths = ["/", "/Leaderboard", "/Home/Legal"];

    /// <summary>Vrai si la page est publique et sans paramètre de requête (hors doublons de contenu).</summary>
    public static bool IsIndexable(string? path, bool hasQueryString)
    {
        if (hasQueryString || string.IsNullOrEmpty(path))
            return false;

        var normalized = path.Length > 1 ? path.TrimEnd('/') : path;
        return IndexablePaths.Any(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase));
    }
}
