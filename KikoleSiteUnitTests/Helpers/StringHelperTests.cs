using System.Collections.Generic;
using FluentAssertions;
using KikoleSite.Helpers;
using KikoleSite.Models.Enums;
using Xunit;

namespace KikoleSiteUnitTests.Helpers
{
    public class StringHelperTests
    {
        // ------------------------------------------------------------- RemoveDiacritics

        [Theory]
        [InlineData("Zinédine Zidane", "Zinedine Zidane")]
        [InlineData("Bayern München", "Bayern Munchen")]
        [InlineData("Atlético Madrid", "Atletico Madrid")]
        [InlineData("Ibrahimović", "Ibrahimovic")]
        public void RemoveDiacritics_StripsLatinAccents(string input, string expected)
        {
            input.RemoveDiacritics().Should().Be(expected);
        }

        [Theory]
        [InlineData("Ødegaard", "Odegaard")]
        [InlineData("Håland", "Haland")]
        [InlineData("Łukasz Fabiański", "Lukasz Fabianski")]
        [InlineData("Beşiktaş", "Besiktas")]
        public void RemoveDiacritics_HandlesLettersWithoutUnicodeDecomposition(string input, string expected)
        {
            // ø et ł n'ont pas de decomposition canonique : seul le best-fit de la page
            // de code les rabat sur leur equivalent ASCII
            input.RemoveDiacritics().Should().Be(expected);
        }

        [Theory]
        [InlineData("Weiß", "Weiss")]
        [InlineData("Großkreutz", "Grosskreutz")]
        [InlineData("Guðjohnsen", "Gudjohnsen")]
        [InlineData("Sigurðsson", "Sigurdsson")]
        [InlineData("Þórir", "thorir")]
        public void RemoveDiacritics_HandlesLettersMissingFromCodePage(string input, string expected)
        {
            // ces lettres ne sont ni decomposables ni presentes dans la table best-fit :
            // sans la table de correspondance explicite elles deviendraient des '?'
            input.RemoveDiacritics().Should().Be(expected);
        }

        [Theory]
        [InlineData("Nguyễn Công Phượng", "Nguyen Cong Phuong")]
        [InlineData("Đặng Văn Lâm", "Dang Van Lam")]
        [InlineData("Ștefan Radu", "Stefan Radu")]
        public void RemoveDiacritics_HandlesExtendedLatinRanges(string input, string expected)
        {
            // plage Latin Extended Additional (vietnamien) : traitee par la normalisation NFD
            input.RemoveDiacritics().Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Ronaldo")]
        [InlineData("O'Neill")]
        [InlineData("Nedved 1972")]
        public void RemoveDiacritics_LeavesAsciiUntouched(string input)
        {
            input.RemoveDiacritics().Should().Be(input);
        }

        // ------------------------------------------------------------- Sanitize

        [Theory]
        [InlineData("  Zinédine Zidane  ", "zinedine zidane")]
        [InlineData("ZIDANE", "zidane")]
        [InlineData("Bayern München", "bayern munchen")]
        public void Sanitize_TrimsLowercasesAndStripsAccents(string input, string expected)
        {
            input.Sanitize().Should().Be(expected);
        }

        [Fact]
        public void Sanitize_IsIdempotent()
        {
            // propriete essentielle : les valeurs stockees en base sont deja sanitisees,
            // les resanitiser ne doit rien changer
            const string input = "  Łukasz Fabiański  ";
            var once = input.Sanitize();
            once.Sanitize().Should().Be(once);
        }

        [Fact]
        public void Sanitize_AccentedAndUnaccentedInputsConverge()
        {
            // c'est la garantie qui permet a un joueur de saisir un nom sans ses accents
            "Zinédine Zidane".Sanitize().Should().Be("Zinedine Zidane".Sanitize());
            "Bayern München".Sanitize().Should().Be("Bayern Munchen".Sanitize());
            "Ødegaard".Sanitize().Should().Be("Odegaard".Sanitize());
        }

        // ------------------------------------------------------------- Disjoin / SanitizeJoin

        [Fact]
        public void Disjoin_SplitsOnSemiColon()
        {
            "zidane;zizou;zinedine zidane"
                .Disjoin()
                .Should()
                .BeEquivalentTo(new[] { "zidane", "zizou", "zinedine zidane" });
        }

        [Fact]
        public void SanitizeJoin_SanitizesAliasesThenAppendsSourceName()
        {
            var result = new List<string> { "Zizou", "ZIDANE" }.SanitizeJoin("Zinédine Zidane");

            result.Should().Be("zizou;zidane;zinedine zidane");
        }

        [Fact]
        public void SanitizeJoin_RemovesDuplicatesAfterSanitization()
        {
            // "Zidane" et "zidane" se confondent une fois sanitises
            var result = new List<string> { "Zidane", "zidane" }.SanitizeJoin("Zidane");

            result.Should().Be("zidane");
        }

        // ------------------------------------------------------------- ContainsSanitized

        [Theory]
        [InlineData("real;real madrid", "Real Madrid", true)]
        [InlineData("real;real madrid", "  REAL  ", true)]
        [InlineData("real;real madrid", "Réal Madrid", true)]     // accent parasite absorbe
        [InlineData("real;real madrid", "Real Madri", false)]     // correspondance exacte : pas de tolerance
        [InlineData("real;real madrid", "Barcelone", false)]
        public void ContainsSanitized_RequiresExactMatchAfterSanitization(string source, string value, bool expected)
        {
            source.ContainsSanitized(value).Should().Be(expected);
        }

        // ------------------------------------------------------------- ContainsApproximately

        [Theory]
        [InlineData("zidane;zinedine zidane", "Zidane", true)]
        [InlineData("zidane;zinedine zidane", "Zinédine Zidane", true)]
        [InlineData("zidane;zinedine zidane", "Zidan", true)]      // 1 erreur sur 5 = 0.20
        [InlineData("zidane;zinedine zidane", "Zizou", false)]
        [InlineData("zidane;zinedine zidane", "Ronaldo", false)]
        public void ContainsApproximately_ToleratesTyposUnderThreshold(string source, string value, bool expected)
        {
            source.ContainsApproximately(value).Should().Be(expected);
        }

        [Fact]
        public void ContainsApproximately_ToleranceIsRelativeToInputLength()
        {
            // le seuil est un ratio (< 0.5), donc un mot court tolere moins d'erreurs
            // qu'un mot long en valeur absolue
            "pele".ContainsApproximately("pel").Should().BeTrue();      // 1/3  = 0.33
            "pele".ContainsApproximately("po").Should().BeFalse();      // 3/2  = 1.50
        }

        // ------------------------------------------------------------- GetLevenshteinDistance

        [Theory]
        [InlineData("", "", 0)]
        [InlineData("abc", "", 3)]
        [InlineData("", "abc", 3)]
        [InlineData("abc", "abc", 0)]
        [InlineData("abc", "abd", 1)]        // substitution
        [InlineData("abc", "ab", 1)]         // suppression
        [InlineData("abc", "abcd", 1)]       // insertion
        [InlineData("kitten", "sitting", 3)] // cas d'ecole
        public void GetLevenshteinDistance_ComputesEditDistance(string s, string t, int expected)
        {
            s.GetLevenshteinDistance(t).Should().Be(expected);
        }

        [Fact]
        public void GetLevenshteinDistance_IsSymmetric()
        {
            "zidane".GetLevenshteinDistance("zidan")
                .Should().Be("zidan".GetLevenshteinDistance("zidane"));
        }

        // ------------------------------------------------------------- IsValid

        [Fact]
        public void IsValid_WhenNull_ReturnsFalse()
        {
            ((IReadOnlyList<string>)null).IsValid().Should().BeFalse();
        }

        [Fact]
        public void IsValid_WhenEmpty_ReturnsFalse()
        {
            new List<string>().IsValid().Should().BeFalse();
        }

        [Fact]
        public void IsValid_WhenAnyEntryIsBlank_ReturnsFalse()
        {
            new List<string> { "zidane", "   " }.IsValid().Should().BeFalse();
        }

        [Fact]
        public void IsValid_WhenAllEntriesAreFilled_ReturnsTrue()
        {
            new List<string> { "zidane", "zizou" }.IsValid().Should().BeTrue();
        }

        // ------------------------------------------------------------- IsEnumValue

        [Theory]
        [InlineData("Goalkeeper", true)]
        [InlineData("1", true)]
        [InlineData("4", true)]
        [InlineData("99", false)]
        [InlineData("Sweeper", false)]
        [InlineData(null, false)]
        public void IsEnumValue_ValidatesNameOrNumericValue(string value, bool expected)
        {
            value.IsEnumValue<Positions>().Should().Be(expected);
        }
    }
}
