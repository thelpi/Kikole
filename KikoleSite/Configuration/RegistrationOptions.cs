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
}
