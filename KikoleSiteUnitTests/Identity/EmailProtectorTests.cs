using System.Collections.Generic;
using FluentAssertions;
using KikoleSite.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace KikoleSiteUnitTests.Identity;

/// <summary>
/// Chiffrement (AES-GCM, pour l'exploitation en cas de fraude) et empreinte de recherche
/// (HMAC-SHA256, pour la connexion par email et l'unicite) des adresses email. Les deux
/// derivent d'une seule cle de configuration ("EmailEncryptionKey"), separee de celle des
/// mots de passe.
/// </summary>
public class EmailProtectorTests
{
    private const string MasterKey = "TestEmailEncryptionKey";

    private static EmailProtector CreateProtector()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["EmailEncryptionKey"] = MasterKey })
            .Build();

        return new EmailProtector(configuration, new SanitizingLookupNormalizer());
    }

    [Fact]
    public void Encrypt_ThenDecrypt_RoundTrips()
    {
        var protector = CreateProtector();

        var cipherText = protector.Encrypt("Zinedine@Example.com");

        protector.Decrypt(cipherText).Should().Be("Zinedine@Example.com");
    }

    [Fact]
    public void Encrypt_IsNotDeterministic()
    {
        // AES-GCM avec un nonce aleatoire a chaque appel : deux chiffrements de la meme
        // valeur ne doivent jamais produire le meme texte chiffre (sinon deux comptes avec
        // la meme adresse seraient reperables par simple comparaison des colonnes chiffrees)
        var protector = CreateProtector();

        var first = protector.Encrypt("zidane@example.com");
        var second = protector.Encrypt("zidane@example.com");

        first.Should().NotBe(second);
        protector.Decrypt(first).Should().Be("zidane@example.com");
        protector.Decrypt(second).Should().Be("zidane@example.com");
    }

    [Fact]
    public void Hash_IsDeterministic()
    {
        // a l'inverse du chiffrement : la recherche/l'unicite en base l'exige
        var protector = CreateProtector();

        protector.Hash("zidane@example.com").Should().Be(protector.Hash("zidane@example.com"));
    }

    [Fact]
    public void Hash_IgnoresCaseAndSurroundingSpaces()
    {
        // meme normalisation que le reste du projet (SanitizingLookupNormalizer) : deux
        // variantes de casse/espaces d'une meme adresse ne doivent pas creer deux comptes
        var protector = CreateProtector();

        protector.Hash("  Zidane@Example.com  ").Should().Be(protector.Hash("zidane@example.com"));
    }

    [Fact]
    public void Hash_DistinguishesGenuinelyDifferentAddresses()
    {
        var protector = CreateProtector();

        protector.Hash("zidane@example.com").Should().NotBe(protector.Hash("zizou@example.com"));
    }
}
