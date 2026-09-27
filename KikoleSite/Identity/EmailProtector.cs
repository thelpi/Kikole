using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Identity;

/// <summary>
/// Chiffre/dechiffre les adresses email (colonne <c>email_encrypted</c>, lisible par un
/// administrateur en cas de fraude) et calcule leur empreinte de recherche (colonne
/// <c>email_hash</c>, utilisee pour la connexion par email, l'unicite, et la detection de
/// doublons sans avoir a dechiffrer). Cle dediee ("EmailEncryptionKey"), separee
/// d'"EncryptionKey" (mots de passe) : contrairement aux cles Data Protection par defaut
/// d'ASP.NET (liees au profil Windows en local), elle est portable d'une machine a l'autre.
/// </summary>
public interface IEmailProtector
{
    /// <summary>Chiffre l'adresse telle que saisie (accents/casse conserves).</summary>
    string Encrypt(string email);

    /// <summary>Dechiffre une valeur produite par <see cref="Encrypt"/>.</summary>
    string Decrypt(string cipherText);

    /// <summary>
    /// Empreinte de recherche : normalise l'adresse (meme regle que
    /// <see cref="ILookupNormalizer.NormalizeEmail"/>) puis la hache avec la sous-cle MAC.
    /// Deterministe - meme email, meme empreinte -, contrairement a <see cref="Encrypt"/>.
    /// </summary>
    string Hash(string email);
}

public class EmailProtector : IEmailProtector
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _macKey;
    private readonly ILookupNormalizer _normalizer;

    public EmailProtector(IConfiguration configuration, ILookupNormalizer normalizer)
    {
        var masterKey = configuration.GetValue<string>("EmailEncryptionKey")
            ?? throw new InvalidOperationException("La cle 'EmailEncryptionKey' est absente de la configuration.");

        // deux sous-cles derivees d'un seul secret configure (chiffrement / signature)
        // plutot que deux secrets separes a gerer, meme principe que le hachage historique
        // des mots de passe (SHA256(valeur + cle)), cf. LegacyCompatiblePasswordHasher.
        _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes($"{masterKey}:enc"));
        _macKey = SHA256.HashData(Encoding.UTF8.GetBytes($"{masterKey}:mac"));
        _normalizer = normalizer;
    }

    public string Encrypt(string email)
    {
        var plainBytes = Encoding.UTF8.GetBytes(email);
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using (var aes = new AesGcm(_encryptionKey, AesGcm.TagByteSizes.MaxSize))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // nonce + tag + texte chiffre, concatenes puis encodes une seule fois
        var payload = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText)
    {
        var payload = Convert.FromBase64String(cipherText);

        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = payload[..nonceSize];
        var tag = payload[nonceSize..(nonceSize + tagSize)];
        var cipherBytes = payload[(nonceSize + tagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using (var aes = new AesGcm(_encryptionKey, tagSize))
        {
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    public string Hash(string email)
    {
        var normalized = _normalizer.NormalizeEmail(email) ?? string.Empty;
        var hashBytes = HMACSHA256.HashData(_macKey, Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexStringLower(hashBytes);
    }
}
