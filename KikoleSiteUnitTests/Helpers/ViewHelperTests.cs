using System;
using System.Globalization;
using FluentAssertions;
using KikoleSite.Helpers;
using KikoleSite.Models.Enums;
using Xunit;

namespace KikoleSiteUnitTests.Helpers
{
    /// <summary>
    /// Tout ce fichier depend de CultureInfo.CurrentCulture. C'est le point du code le
    /// plus expose a un changement de version de .NET (evolutions d'ICU), d'ou une
    /// couverture des sorties exactes plutot que de la seule logique.
    /// </summary>
    public class ViewHelperTests
    {
        /// <summary>Fixe la culture courante et la restaure a la sortie du bloc.</summary>
        private sealed class Culture : IDisposable
        {
            private readonly CultureInfo _previous;

            private Culture(string name)
            {
                _previous = CultureInfo.CurrentCulture;
                CultureInfo.CurrentCulture = new CultureInfo(name);
            }

            public static Culture French() => new Culture("fr");
            public static Culture English() => new Culture("en");

            public void Dispose() => CultureInfo.CurrentCulture = _previous;
        }

        // ------------------------------------------------------------- dates

        [Fact]
        public void ToNaString_FormatsDatesPerCulture()
        {
            var date = new DateTime(2026, 9, 2);

            using (Culture.French())
                date.ToNaString().Should().Be("02/09/2026");

            using (Culture.English())
                date.ToNaString().Should().Be("2026-09-02");
        }

        [Fact]
        public void ToStringHour_AppendsATwentyFourHourTime()
        {
            var moment = new DateTime(2026, 9, 2, 18, 5, 30);

            using (Culture.French())
                moment.ToStringHour().Should().Be("02/09/2026 18:05:30");

            using (Culture.English())
                moment.ToStringHour().Should().Be("2026-09-02 18:05:30");
        }

        [Fact]
        public void GetNumDayLabel_DropsTheYear()
        {
            var date = new DateTime(2026, 9, 2);

            using (Culture.French())
                date.GetNumDayLabel().Should().Be("02/09");

            using (Culture.English())
                date.GetNumDayLabel().Should().Be("09-02");
        }

        [Fact]
        public void ToNaString_WhenTheDateIsNull_ReturnsNa()
        {
            ((DateTime?)null).ToNaString().Should().Be(ViewHelper.NA);
        }

        // ------------------------------------------------------------- durees

        [Theory]
        [InlineData(0, 45, "00:45")]
        [InlineData(9, 5, "09:05")]
        [InlineData(23, 59, "23:59")]
        public void ToNaString_FormatsDurationsUnderADayAsHoursAndMinutes(int hours, int minutes, string expected)
        {
            // le motif echappe le deux-points, il ne depend donc pas du separateur culturel
            var span = new TimeSpan(hours, minutes, 0);

            using (Culture.French())
                span.ToNaString().Should().Be(expected);

            using (Culture.English())
                span.ToNaString().Should().Be(expected);
        }

        [Theory]
        [InlineData(24, "1 jour", "1 day")]
        [InlineData(48, "2 jours", "2 days")]
        [InlineData(72, "3 jours", "3 days")]
        [InlineData(47, "1 jour", "1 day")]   // troncature vers le bas
        public void ToNaString_BeyondADaySwitchesToDaysAndPluralises(int hours, string fr, string en)
        {
            var span = TimeSpan.FromHours(hours);

            using (Culture.French())
                span.ToNaString().Should().Be(fr);

            using (Culture.English())
                span.ToNaString().Should().Be(en);
        }

        [Fact]
        public void ToNaString_WhenTheDurationIsNull_ReturnsNa()
        {
            ((TimeSpan?)null).ToNaString().Should().Be(ViewHelper.NA);
        }

        // ------------------------------------------------------------- booleens

        [Fact]
        public void ToYesNo_IsTranslated()
        {
            using (Culture.French())
            {
                true.ToYesNo().Should().Be("Oui");
                false.ToYesNo().Should().Be("Non");
            }

            using (Culture.English())
            {
                true.ToYesNo().Should().Be("Yes");
                false.ToYesNo().Should().Be("No");
            }
        }

        [Fact]
        public void ToYesNo_WhenNull_ReturnsNa()
        {
            ((bool?)null).ToYesNo().Should().Be(ViewHelper.NA);
        }

        // ------------------------------------------------------------- ToNaString(object)

        [Fact]
        public void ToNaString_OnObject_DispatchesOnTheRuntimeType()
        {
            using (Culture.English())
            {
                ((object)null).ToNaString().Should().Be(ViewHelper.NA);
                ((object)new DateTime(2026, 9, 2)).ToNaString().Should().Be("2026-09-02");
                ((object)new TimeSpan(1, 30, 0)).ToNaString().Should().Be("01:30");
                ((object)true).ToNaString().Should().Be("Yes");
                ((object)42).ToNaString().Should().Be("42");
            }
        }

        [Fact]
        public void ToNaString_OnObject_ABoxedNullableIsSeenAsItsUnderlyingType()
        {
            // GetType() sur un nullable boxe renvoie le type sous-jacent : les branches
            // typeof(TimeSpan?) et typeof(DateTime?) du code sont donc inatteignables,
            // mais le comportement reste correct car les branches non nullables couvrent
            using (Culture.English())
            {
                object boxed = (DateTime?)new DateTime(2026, 9, 2);
                boxed.ToNaString().Should().Be("2026-09-02");
            }
        }

        // ------------------------------------------------------------- libelles

        [Fact]
        public void GetMonthName_UsesHardcodedNamesNotTheCulture()
        {
            // choix deliberé du code : les noms de mois ne viennent pas de CultureInfo,
            // ils sont ecrits en dur. C'est ce qui les rend insensibles a une evolution
            // d'ICU lors d'une montee de version .NET.
            var august = new DateTime(2026, 8, 1);

            using (Culture.French())
                august.GetMonthName().Should().Be("Août");

            using (Culture.English())
                august.GetMonthName().Should().Be("August");
        }

        [Fact]
        public void GetMonthName_CoversTheTwelveMonths()
        {
            using (Culture.French())
            {
                for (var month = 1; month <= 12; month++)
                {
                    var name = new DateTime(2026, month, 1).GetMonthName();
                    name.Should().NotBeNullOrWhiteSpace();
                }
            }
        }

        [Theory]
        [InlineData(ProposalTypes.Name, "de nom", "nom")]
        [InlineData(ProposalTypes.Club, "de club", "club")]
        [InlineData(ProposalTypes.Year, "d'année", "année")]
        [InlineData(ProposalTypes.Country, "de nationalité", "nationalité")]
        public void GetLabel_HandlesTheFrenchElision(ProposalTypes type, string withDe, string without)
        {
            using (Culture.French())
            {
                type.GetLabel(true).Should().Be(withDe);
                type.GetLabel(false).Should().Be(without);
            }
        }

        [Fact]
        public void GetLabel_InEnglishTheArticleFlagIsIgnored()
        {
            using (Culture.English())
            {
                ProposalTypes.Year.GetLabel(true).Should().Be("year");
                ProposalTypes.Year.GetLabel(false).Should().Be("year");
            }
        }

        [Fact]
        public void GetSimpleLabel_InEnglishFallsBackToTheEnumName()
        {
            using (Culture.English())
                ProposalTypes.Leaderboard.GetSimpleLabel().Should().Be("Leaderboard");

            using (Culture.French())
                ProposalTypes.Leaderboard.GetSimpleLabel().Should().Be("Classement");
        }

        [Fact]
        public void GetSimpleLabel_CoversEveryProposalType()
        {
            // garde-fou : la methode leve NotImplementedException sur un type inconnu
            using (Culture.French())
            {
                foreach (ProposalTypes type in Enum.GetValues(typeof(ProposalTypes)))
                    type.GetSimpleLabel().Should().NotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public void GetLabel_CoversEveryPositionAndSort()
        {
            using (Culture.French())
            {
                foreach (Positions position in Enum.GetValues(typeof(Positions)))
                    position.GetLabel().Should().NotBeNullOrWhiteSpace();

                foreach (LeaderSorts sort in Enum.GetValues(typeof(LeaderSorts)))
                    sort.GetLabel().Should().NotBeNullOrWhiteSpace();

                foreach (DayLeaderSorts sort in Enum.GetValues(typeof(DayLeaderSorts)))
                    sort.GetLabel().Should().NotBeNullOrWhiteSpace();
            }
        }

        // ------------------------------------------------------------- selection de langue

        [Fact]
        public void GetLanguage_MapsTheCurrentCultureOntoTheSupportedLanguages()
        {
            using (Culture.French())
                ViewHelper.GetLanguage().Should().Be(Languages.fr);

            using (Culture.English())
                ViewHelper.GetLanguage().Should().Be(Languages.en);
        }

        [Fact]
        public void GetLanguage_AnyOtherCultureFallsBackToEnglish()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de");
                ViewHelper.GetLanguage().Should().Be(Languages.en);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public void FrenchIsDetectedOnTheNeutralLanguageNotTheFullCulture()
        {
            // la detection utilise TwoLetterISOLanguageName : fr-CA doit compter comme
            // du francais au meme titre que fr
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
                ViewHelper.GetLanguage().Should().Be(Languages.fr);
                true.ToYesNo().Should().Be("Oui");
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
