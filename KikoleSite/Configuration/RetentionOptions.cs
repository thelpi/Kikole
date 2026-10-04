namespace KikoleSite.Configuration;

/// <summary>Section <c>Retention</c> de la configuration : durées de conservation des données personnelles.</summary>
public record RetentionOptions
{
    /// <summary>
    /// Durée, en mois, de conservation des adresses IP (inscription, historique de connexion,
    /// propositions de jeu), annoncée telle quelle dans les mentions légales.
    /// </summary>
    public int IpMonths { get; init; } = 12;
}