using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Identity;

/// <summary>
/// Hacheur de mots de passe (et de reponses de securite : meme algorithme, secret
/// different) qui reconnait encore l'ancien format SHA256+sel global. Un hash reconnu
/// comme ancien declenche <see cref="PasswordVerificationResult.SuccessRehashNeeded"/>,
/// qu'Identity traduit en reecriture automatique au format PBKDF2 a la prochaine
/// connexion reussie — sans reset force ni interruption de service.
/// </summary>
public class LegacyCompatiblePasswordHasher : IPasswordHasher<ApplicationUser>
{
    private const int LegacyHashLength = 64;

    private readonly PasswordHasher<ApplicationUser> _modernHasher = new();
    private readonly string _legacyEncryptionKey;

    public LegacyCompatiblePasswordHasher(IConfiguration configuration)
    {
        _legacyEncryptionKey = configuration.GetValue<string>("EncryptionKey")
            ?? throw new InvalidOperationException("La cle 'EncryptionKey' est absente de la configuration.");
    }

    public string HashPassword(ApplicationUser user, string password)
        => _modernHasher.HashPassword(user, password);

    public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        if (!IsLegacyHash(hashedPassword))
            return _modernHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        var legacyHash = ComputeLegacyHash(providedPassword);
        return string.Equals(legacyHash, hashedPassword, StringComparison.OrdinalIgnoreCase)
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }

    private static bool IsLegacyHash(string hash)
        => hash.Length == LegacyHashLength && hash.All(Uri.IsHexDigit);

    private string ComputeLegacyHash(string value)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{value.Trim()}{_legacyEncryptionKey}"));

        var sb = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
}
