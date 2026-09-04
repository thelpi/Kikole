using FluentAssertions;
using KikoleSite.Identity;
using Xunit;

namespace KikoleSiteUnitTests.Identity;

public class SanitizingLookupNormalizerTests
{
    private readonly SanitizingLookupNormalizer _normalizer = new();

    [Theory]
    [InlineData("Joueur1", "JOUEUR1")]
    [InlineData("  Réné  ", "RENE")]
    [InlineData("JOUEUR1", "JOUEUR1")]
    public void NormalizeName_SanitizesThenUppercases(string input, string expected)
    {
        _normalizer.NormalizeName(input).Should().Be(expected);
    }

    [Fact]
    public void NormalizeName_OfNull_IsNull()
    {
        _normalizer.NormalizeName(null).Should().BeNull();
    }

    [Fact]
    public void NormalizeName_TwoLoginsDifferingOnlyByAccentsOrCase_NormalizeToTheSameValue()
    {
        _normalizer.NormalizeName("joueur1").Should().Be(_normalizer.NormalizeName("Jôueur1"));
    }
}
