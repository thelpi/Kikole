using System.Collections.Generic;
using FluentAssertions;
using KikoleSite.Helpers;
using KikoleSite.Models.Enums;
using Xunit;

namespace KikoleSiteUnitTests.Helpers;

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

    // ------------------------------------------------------------- SanitizeForSearch

    [Theory]
    [InlineData("Saint-Étienne", "saintetienne")]
    [InlineData("saint etienne", "saintetienne")]
    [InlineData("Milan A.C.", "milanac")]
    [InlineData("milan ac", "milanac")]
    [InlineData("Côte d'Ivoire", "cotedivoire")]
    [InlineData("  -.  ", "")]
    public void SanitizeForSearch_KeepsOnlyLettersAndDigits(string input, string expected)
    {
        input.SanitizeForSearch().Should().Be(expected);
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

    [Theory]
    // les faux positifs rapportes : des noms voisins ou plus courts qui passaient
    [InlineData("luka modric;modric", "Rodri", false)]
    [InlineData("pedro", "Pedri", false)]
    [InlineData("ronaldo", "Ronaldinho", false)]
    [InlineData("xavi;xavi hernandez", "Xabi", false)]
    // une faute de frappe reste toleree des 6 lettres
    [InlineData("luka modric;modric", "Modrik", true)]
    [InlineData("luka modric;modric", "Modrc", true)]
    [InlineData("luka modric;modric", "Luka Modrik", true)]
    [InlineData("zidane", "Zidnae", true)]
    // exact toujours accepte, accents et casse ignores
    [InlineData("luka modric;modric", "Modrić", true)]
    [InlineData("pele", "Pelé", true)]
    // pas de faute toleree jusqu'a 5 lettres
    [InlineData("pele", "pel", false)]
    [InlineData("pedro", "pedr", false)]
    public void ContainsApproximately_ToleratesAFewTyposButNotOtherNames(string source, string value, bool expected)
    {
        source.ContainsApproximately(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(6, 1)]
    [InlineData(10, 1)]
    [InlineData(11, 2)]
    [InlineData(15, 2)]
    [InlineData(16, 3)]
    public void ContainsApproximately_AllowsOneMoreTypoPerFiveLetters(int length, int allowedTypos)
    {
        var name = new string('a', length);
        var withAllowedTypos = new string('a', length - allowedTypos) + new string('b', allowedTypos);
        var withOneMore = new string('a', length - allowedTypos - 1) + new string('b', allowedTypos + 1);

        name.ContainsApproximately(withAllowedTypos).Should().BeTrue();
        name.ContainsApproximately(withOneMore).Should().BeFalse();
    }

    // ------------------------------------------------------------- IsSimilarName

    [Theory]
    // fautes de frappe, meme tolerance que la bonne reponse
    [InlineData("Ziddane", "Zinedine Zidane", true)]
    [InlineData("Zinedine Ziddane", "Zinedine Zidane", true)]
    [InlineData("Zinedinezidane", "Zinedine Zidane", true)]
    // nom seul, prenom seul, prenom et nom inverses
    [InlineData("Zidane", "Zinedine Zidane", true)]
    [InlineData("Zinedine Zidane", "Zidane", true)]
    [InlineData("Zinedine", "Zinedine Zidane", true)]
    [InlineData("Zidane Zinedine", "Zinedine Zidane", true)]
    [InlineData("Ziddane Zinedine", "Zinedine Zidane", true)]
    // initiale, ponctuation, accents
    [InlineData("C. Ronaldo", "Cristiano Ronaldo", true)]
    [InlineData("Zinédine-Zidane", "Zinedine Zidane", true)]
    // des joueurs differents ne se ressemblent pas, meme avec un prenom ou un nom commun
    [InlineData("Ronaldo", "Ronaldinho", false)]
    [InlineData("Pedro", "Pedri", false)]
    [InlineData("Thierry Henry", "Thierry Sagnol", false)]
    [InlineData("Eden Hazard", "Thorgan Hazard", false)]
    [InlineData("Luis Suarez", "Luis Enrique", false)]
    [InlineData("Rodri", "Modric", false)]
    [InlineData("", "Zidane", false)]
    public void IsSimilarName_FollowsTheSameToleranceAsTheCorrectAnswer(string value, string other, bool expected)
    {
        value.IsSimilarName(other).Should().Be(expected);
        other.IsSimilarName(value).Should().Be(expected);
    }

    [Fact]
    public void FindSimilarName_ReturnsTheClosestPreviousName()
    {
        var previous = new[] { "Zinedine Zidane", "Zidane Zinedine", "Kylian Mbappe" };

        previous.FindSimilarName("Zinedine Ziddane").Should().Be("Zinedine Zidane");
    }

    [Theory]
    [InlineData("zinedine zidane")]
    [InlineData("  ZINÉDINE ZIDANE ")]
    public void FindSimilarName_IgnoresTheSameProposalAlreadyBilledOnce(string value)
    {
        new[] { "Zinedine Zidane" }.FindSimilarName(value).Should().BeNull();
    }

    [Fact]
    public void FindSimilarName_WithoutAnyPreviousName_ReturnsNull()
    {
        new List<string>().FindSimilarName("Zidane").Should().BeNull();
    }

    // ------------------------------------------------------------- GetEditDistance

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("abc", "", 3)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "abc", 0)]
    [InlineData("abc", "abd", 1)]        // substitution
    [InlineData("abc", "ab", 1)]         // suppression
    [InlineData("abc", "abcd", 1)]       // insertion
    [InlineData("abcd", "abdc", 1)]      // inversion de deux lettres adjacentes
    [InlineData("kitten", "sitting", 3)] // cas d'ecole
    [InlineData("modric", "rodri", 2)]
    public void GetEditDistance_ComputesEditDistance(string s, string t, int expected)
    {
        s.GetEditDistance(t).Should().Be(expected);
    }

    [Fact]
    public void GetEditDistance_IsSymmetric()
    {
        "zidane".GetEditDistance("zidan")
            .Should().Be("zidan".GetEditDistance("zidane"));
    }
    // ------------------------------------------------------------- IsValid

    [Fact]
    public void IsValid_WhenNull_ReturnsFalse()
    {
        ((IReadOnlyList<string>?)null).IsValid().Should().BeFalse();
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
    public void IsEnumValue_ValidatesNameOrNumericValue(string? value, bool expected)
    {
        value.IsEnumValue<Positions>().Should().Be(expected);
    }
    // ------------------------------------------------------------- caracteres non latins

    [Theory]
    [InlineData("山田太郎", "山田太郎")]
    [InlineData("Иван Петров", "иван петров")]
    [InlineData("서울", "서울")]
    [InlineData("ひらがなだ", "ひらがなだ")]
    [InlineData("Yamada 山田", "yamada 山田")]
    public void Sanitize_KeepsNonLatinTextInsteadOfReplacingItWithQuestionMarks(string input, string expected)
    {
        input.Sanitize().Should().Be(expected);
    }

    [Fact]
    public void Sanitize_FoldsFullWidthLatinLetters()
    {
        "ＹＡＭＡＤＡ　Ｔａｒｏ".Sanitize().Should().Be("yamada taro");
    }

    [Fact]
    public void SanitizeJoin_KeepsTheJapaneseAliasNextToTheLatinName()
    {
        new[] { "山田太郎" }.SanitizeJoin("Yamada Taro").Should().Be("山田太郎;yamada taro");
    }

    [Theory]
    [InlineData("山田太郎", true)]
    [InlineData("yamada taro", true)]
    [InlineData("田中花子", false)]
    [InlineData("Иван", false)]
    [InlineData("????", false)]
    public void ContainsApproximately_JapaneseAlias_OnlyMatchesTheSameName(string guess, bool expected)
    {
        "yamada taro;山田太郎".ContainsApproximately(guess).Should().Be(expected);
    }

    [Theory]
    [InlineData("\u0301")]
    [InlineData("   ")]
    public void ContainsApproximately_WhenNothingIsLeftAfterNormalization_DoesNotMatchNorThrow(string guess)
    {
        "zidane;zinedine zidane".ContainsApproximately(guess).Should().BeFalse();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("????", false)]
    [InlineData("?? ??", false)]
    [InlineData("   ", false)]
    [InlineData("zidane", true)]
    [InlineData("山田太郎", true)]
    [InlineData("?a?", true)]
    public void HasReadableText_IsFalseForAliasesDamagedByTheLegacyNormalization(string value, bool expected)
    {
        value.HasReadableText().Should().Be(expected);
    }
}
