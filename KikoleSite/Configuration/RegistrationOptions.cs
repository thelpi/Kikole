using System.Collections.Generic;

namespace KikoleSite.Configuration;

/// <summary>
/// Section <c>Registration</c> de la configuration, liée via <c>IOptions&lt;T&gt;</c>
/// plutôt qu'un <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> brut
/// injecté partout : les clés attendues sont visibles au typage, pas dispersées en
/// chaînes de caractères dans chaque classe qui en a besoin.
/// </summary>
public record RegistrationOptions
{
    /// <summary>
    /// Inscription sur invitation (<c>registration_guids</c>) plutôt que libre. Le
    /// mécanisme reste en place quand c'est à <c>false</c> — seule la validation du GUID
    /// est court-circuitée — pour pouvoir le remettre par une simple bascule de config.
    /// </summary>
    public bool InviteEnabled { get; init; }

    /// <summary>
    /// Nombre maximal de comptes créés depuis une même IP sur les dernières 24h.
    /// <c>null</c> désactive la vérification.
    /// </summary>
    public int? MaxCreationsPerIpPerDay { get; init; }

    /// <summary>
    /// IP exemptées de <see cref="MaxCreationsPerIpPerDay"/> (ex. un réseau de bureau où
    /// plusieurs joueurs légitimes partagent la même IP sortante).
    /// </summary>
    public IReadOnlyList<string> RateLimitWhitelistedIps { get; init; } = [];
}
