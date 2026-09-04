using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace KikoleSite.Identity;

/// <summary>
/// Rejette les mots de passe presents dans des fuites connues, via l'API Have I Been
/// Pwned en k-anonymity : seuls les 5 premiers caracteres du hash SHA1 du mot de passe
/// quittent le serveur (le mot de passe et son hash complet ne sortent jamais).
///
/// Indisponibilite ou erreur de l'API => on laisse passer (repli tolerant) : un service
/// tiers en panne ne doit pas empecher un joueur de creer un compte ou de changer de mot
/// de passe.
/// </summary>
public class HibpPasswordValidator : IPasswordValidator<ApplicationUser>
{
    internal const string PwnedPasswordErrorCode = "PwnedPassword";

    private readonly HttpClient _httpClient;

    public HibpPasswordValidator(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(HibpPasswordValidator));
    }

    public async Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        // les autres validateurs (longueur...) se chargent deja du cas vide
        if (string.IsNullOrEmpty(password))
            return IdentityResult.Success;

        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        var prefix = hash[..5];
        var suffix = hash[5..];

        try
        {
            using var response = await _httpClient.GetAsync($"range/{prefix}");

            if (!response.IsSuccessStatusCode)
                return IdentityResult.Success;

            var body = await response.Content.ReadAsStringAsync();

            var isPwned = body
                .Split('\n')
                .Any(line =>
                {
                    var separator = line.IndexOf(':');
                    return separator > 0
                        && line.AsSpan(0, separator).Trim().Equals(suffix, StringComparison.OrdinalIgnoreCase);
                });

            return isPwned
                ? IdentityResult.Failed(new IdentityError
                {
                    Code = PwnedPasswordErrorCode,
                    Description = "This password has appeared in a known data breach."
                })
                : IdentityResult.Success;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return IdentityResult.Success;
        }
    }
}
