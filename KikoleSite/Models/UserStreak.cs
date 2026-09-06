namespace KikoleSite.Models;

/// <summary>
/// Serie de jours consecutifs ou l'utilisateur a trouve le kikole du jour meme
/// (pas en rattrapage sur un jour passe).
/// </summary>
public class UserStreak
{
    /// <summary>
    /// Longueur de la serie en cours (le jour courant, tant qu'il n'est pas encore
    /// joue, ne casse pas une serie qui s'est arretee hier).
    /// </summary>
    public required int Current { get; set; }

    /// <summary>
    /// Plus longue serie jamais realisee (inclut la serie en cours si c'est elle
    /// la plus longue).
    /// </summary>
    public required int Best { get; set; }

    /// <summary>
    /// <c>False</c> tant que l'utilisateur n'a jamais trouve un seul kikole a temps :
    /// dans ce cas, rien ne doit etre affiche plutot que "0 jour".
    /// </summary>
    public bool HasStreak => Best > 0;
}
