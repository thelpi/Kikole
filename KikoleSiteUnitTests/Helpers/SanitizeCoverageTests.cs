using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KikoleSite.Helpers;
using Xunit;

namespace KikoleSiteUnitTests.Helpers;

/// <summary>
/// Couverture de <c>Sanitize</c> sur les alphabets latins. Une lettre que ne ramene ni la
/// decomposition NFD, ni le rabattement de la page de code, ni la table explicite devient
/// '?' : personne ne peut alors la saisir au clavier pour retrouver le joueur.
///
/// Restent volontairement hors perimetre l'alphabet phonetique et les orthographes
/// africaines (Latin Extended-B), ainsi que quelques archaismes de Latin Extended
/// Additional : moyen gallois et variantes du s long.
/// </summary>
public class SanitizeCoverageTests
{
    [Theory]
    [InlineData(0x00C0, 0x00FF)]   // Latin-1 Supplement
    [InlineData(0x0100, 0x017F)]   // Latin Extended-A
    [InlineData(0x1E00, 0x1E99)]   // Latin Extended Additional, partie usuelle
    [InlineData(0x1EA0, 0x1EF9)]   // vietnamien
    public void NoEuropeanLetterIsLost(int from, int to)
    {
        LostLetters(from, to).Should().BeEmpty();
    }

    [Theory]
    [InlineData('Ĳ', "ij")]   // ligature neerlandaise
    [InlineData('ŀ', "l")]    // L point-median catalan
    [InlineData('đ', "d")]    // croate et serbe
    [InlineData('ø', "o")]    // scandinave
    [InlineData('ß', "ss")]
    [InlineData('ế', "e")]    // vietnamien, deux diacritiques superposes
    public void AwkwardLettersReachTheirAsciiForm(char source, string expected)
    {
        source.ToString().Sanitize().Should().Be(expected);
    }

    private static List<string> LostLetters(int from, int to)
    {
        var lost = new List<string>();
        for (var i = from; i <= to; i++)
        {
            var c = (char)i;
            if (!char.IsLetter(c))
                continue;

            var result = c.ToString().Sanitize();
            if (result.Length == 0 || result.Contains('?') || result.Any(r => r > 127))
                lost.Add($"U+{i:X4} {c}");
        }

        return lost;
    }
}
