using System.Collections.Generic;

namespace KikoleSite.Configuration;

/// <summary>
/// Section <c>ForwardedProxy</c> : les reverse proxies/CDN dont on accepte l'en-tete
/// <c>X-Forwarded-For</c>. Vide par defaut (aucun hebergement choisi a ce jour) — le
/// middleware <c>ForwardedHeadersMiddleware</c> ne fait alors confiance qu'a la boucle
/// locale, son comportement natif. Une fois l'infra de production connue, l'IP ou le
/// sous-reseau du proxy s'ajoute ici, sans toucher au code.
/// </summary>
public record ForwardedProxyOptions
{
    /// <summary>Adresses IP exactes des proxies de confiance (ex. "10.0.0.5").</summary>
    public IReadOnlyList<string> KnownProxies { get; init; } = [];

    /// <summary>Sous-reseaux de confiance en notation CIDR (ex. "10.0.0.0/24").</summary>
    public IReadOnlyList<string> KnownNetworks { get; init; } = [];
}
