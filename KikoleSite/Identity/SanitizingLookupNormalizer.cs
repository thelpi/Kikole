using KikoleSite.Helpers;
using Microsoft.AspNetCore.Identity;

namespace KikoleSite.Identity;

/// <summary>
/// Remplace le normaliseur par defaut d'Identity (simple mise en majuscules) par le meme
/// <see cref="StringHelper.Sanitize"/> deja utilise partout ailleurs dans le projet pour
/// dedupliquer les chaines utilisateur (accents, casse, espaces superflus). Sans ca,
/// "Joueur1" et "Jöueur1" seraient deux comptes distincts.
/// </summary>
public class SanitizingLookupNormalizer : ILookupNormalizer
{
    public string? NormalizeName(string? name) => name?.Sanitize().ToUpperInvariant();

    public string? NormalizeEmail(string? email) => email?.Sanitize().ToUpperInvariant();
}
