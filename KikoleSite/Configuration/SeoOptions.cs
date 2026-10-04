namespace KikoleSite.Configuration;

/// <summary>Section <c>Seo</c> de la configuration.</summary>
public record SeoOptions
{
    /// <summary>
    /// Adresse publique du site, sans barre finale : base des URL canoniques, des balises de
    /// partage, du sitemap et de <c>robots.txt</c>.
    /// </summary>
    public string CanonicalBaseUrl { get; init; } = "https://www.kikole.fr";
}
