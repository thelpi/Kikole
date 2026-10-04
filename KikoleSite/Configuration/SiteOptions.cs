namespace KikoleSite.Configuration;

/// <summary>Section <c>Site</c> de la configuration.</summary>
public record SiteOptions
{
    /// <summary>Adresse email publique de l'éditeur, affichée dans les mentions légales.</summary>
    public string ContactEmail { get; init; } = "admin@kikole.fr";

    /// <summary>Affiche le bouton « Partager mon résultat » après une victoire. Désactivé par défaut.</summary>
    public bool ShareResultEnabled { get; init; }
}