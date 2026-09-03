using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using KikoleSite.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace KikoleSiteUnitTests.Identity;

/// <summary>
/// Verifie la compatibilite avec l'ancien format SHA256+sel (les comptes existants avant
/// la refonte) et le declenchement du rehash automatique, sans casser la verification des
/// hash au format moderne (PBKDF2).
/// </summary>
public class LegacyCompatiblePasswordHasherTests
{
    private const string EncryptionKey = "TestEncryptionKey";

    private static LegacyCompatiblePasswordHasher CreateHasher()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["EncryptionKey"] = EncryptionKey })
            .Build();

        return new LegacyCompatiblePasswordHasher(configuration);
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser { PasswordResetQuestion = "q", PasswordResetAnswerHash = string.Empty };
    }

    private static string LegacyHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{value.Trim()}{EncryptionKey}"));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    [Fact]
    public void VerifyHashedPassword_WithAMatchingLegacyHash_SucceedsAndAsksForARehash()
    {
        var hasher = CreateHasher();
        var legacyHash = LegacyHash("motdepasse");

        var result = hasher.VerifyHashedPassword(CreateUser(), legacyHash, "motdepasse");

        result.Should().Be(PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Fact]
    public void VerifyHashedPassword_WithANonMatchingLegacyHash_Fails()
    {
        var hasher = CreateHasher();
        var legacyHash = LegacyHash("motdepasse");

        var result = hasher.VerifyHashedPassword(CreateUser(), legacyHash, "autrechose");

        result.Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void HashPassword_ThenVerify_SucceedsWithoutRehash()
    {
        var hasher = CreateHasher();
        var user = CreateUser();

        var hash = hasher.HashPassword(user, "motdepasse");

        hasher.VerifyHashedPassword(user, hash, "motdepasse").Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void HashPassword_ThenVerify_WithTheWrongValue_Fails()
    {
        var hasher = CreateHasher();
        var user = CreateUser();

        var hash = hasher.HashPassword(user, "motdepasse");

        hasher.VerifyHashedPassword(user, hash, "autrechose").Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void VerifyHashedPassword_WorksTheSameForTheRecoveryAnswer()
    {
        // meme algorithme, secret different : rien de code en dur sur le mot de passe.
        var hasher = CreateHasher();
        var user = CreateUser();

        var answerHash = hasher.HashPassword(user, "kikole");

        hasher.VerifyHashedPassword(user, answerHash, "kikole").Should().Be(PasswordVerificationResult.Success);
        hasher.VerifyHashedPassword(user, answerHash, "autre").Should().Be(PasswordVerificationResult.Failed);
    }
}
