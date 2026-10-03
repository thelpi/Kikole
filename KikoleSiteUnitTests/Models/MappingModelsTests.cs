using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Xunit;

namespace KikoleSiteUnitTests.Models;

/// <summary>
/// Modeles de presentation construits depuis un DTO. Peu de logique, mais ce sont
/// eux qui traduisent les identifiants numeriques de la base en valeurs typees.
/// </summary>
public class MappingModelsTests
{
    // ------------------------------------------------------------- Club

    [Fact]
    public void Club_GroupsTranslationsByLanguageOrderedByPriority()
    {
        var translations = new[]
        {
            ClubTranslationDtoBuilder.Valid().WithClubId(3).WithLanguage(Languages.fr).WithPriority(0).WithName("Juventus Turin").Build(),
            ClubTranslationDtoBuilder.Valid().WithClubId(3).WithLanguage(Languages.fr).WithPriority(1).WithName("Juve").Build(),
            ClubTranslationDtoBuilder.Valid().WithClubId(3).WithLanguage(Languages.en).WithPriority(0).WithName("Juventus FC").Build()
        };

        var club = new Club(ClubDtoBuilder.Valid().WithId(3).WithName("Juventus Turin").Build(), translations);

        club.Id.Should().Be(3);
        club.Name.Should().Be("Juventus Turin");
        club.GetCanonicalName(Languages.fr).Should().Be("Juventus Turin");
        club.GetCanonicalName(Languages.en).Should().Be("Juventus FC");
        club.NamesByLanguage[Languages.fr].Should().BeEquivalentTo(new[] { "Juventus Turin", "Juve" }, o => o.WithStrictOrdering());
    }

    [Theory]
    [InlineData("avignon", "AC Arles-Avignon")]
    [InlineData("arlésien", "Athletic Club Arlésien")]
    [InlineData("arles", "Athletic Club Arlésien")]
    [InlineData("xyz", null)]
    [InlineData("", null)]
    public void Club_GetMatchingName_ReturnsTheFirstNameContainingTheTermByPriority(string term, string? expected)
    {
        var translations = new[]
        {
            ClubTranslationDtoBuilder.Valid().WithClubId(68).WithLanguage(Languages.fr).WithPriority(0).WithName("Athletic Club Arlésien").Build(),
            ClubTranslationDtoBuilder.Valid().WithClubId(68).WithLanguage(Languages.fr).WithPriority(1).WithName("AC Arles-Avignon").Build()
        };
        var club = new Club(ClubDtoBuilder.Valid().WithId(68).WithName("Athletic Club Arlésien").Build(), translations);

        club.GetMatchingName(Languages.fr, term).Should().Be(expected);
    }

    // ------------------------------------------------------------- PlayerClub

    [Fact]
    public void PlayerClub_ResolvesTheClubNameFromTheCareerEntry()
    {
        var clubs = new List<ClubDto>
        {
            ClubDtoBuilder.Valid().WithId(1).WithName("AS Cannes").Build(),
            ClubDtoBuilder.Valid().WithId(2).WithName("Real Madrid").Build()
        };

        var pc = new PlayerClub(
            new PlayerClubDto { ClubId = 2, HistoryPosition = 4, IsLoan = 1 }, clubs);

        pc.Name.Should().Be("Real Madrid");
        pc.HistoryPosition.Should().Be(4);
        pc.IsLoan.Should().BeTrue();
    }

    [Fact]
    public void PlayerClub_WhenTheClubIsMissing_SaysWhichClubIsMissing()
    {
        // une carriere referencant un club absent du referentiel est une incoherence
        // de donnees : elle leve, en nommant le club en cause
        Action act = () => new PlayerClub(
            new PlayerClubDto { PlayerId = 7, ClubId = 99 }, new List<ClubDto>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*99*");
    }

    // ------------------------------------------------------------- Country / Continent

    [Fact]
    public void Country_ParsesTheIsoCodeIntoItsEnum()
    {
        var country = new Country(CountryDtoBuilder.Valid().WithCode("FRA").WithName("France").Build());

        country.Code.Should().Be(Countries.FRA);
        country.Name.Should().Be("France");
    }

    [Fact]
    public void Country_WhenTheCodeIsUnknown_Throws()
    {
        Action act = () => new Country(CountryDtoBuilder.Valid().WithCode("ZZZ").WithName("Nulle part").Build());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Continent_MapsTheIdentifierOntoItsEnum()
    {
        var continent = new Continent(new ContinentDto { Id = (ulong)Continents.SouthAmerica,
            Name = "Amérique du Sud" });

        continent.Id.Should().Be(Continents.SouthAmerica);
        continent.Name.Should().Be("Amérique du Sud");
    }

    // ------------------------------------------------------------- User / Badge

    [Fact]
    public void User_KeepsOnlyTheIdentityFieldsAndDropsTheCredentials()
    {
        var user = new User(UserDtoBuilder.Valid().WithId(7).WithLogin("joueur").WithPassword("un-hash-secret").WithEmailEncrypted("un-autre-hash").Build());

        user.Id.Should().Be(7);
        user.Login.Should().Be("joueur");
        user.GetType().GetProperties().Select(p => p.Name)
            .Should().BeEquivalentTo(new[] { "Id", "Login" });
    }

    [Theory]
    [InlineData((byte)0, false)]
    [InlineData((byte)1, true)]
    [InlineData((byte)2, true)]
    public void Badge_TreatsAnyNonZeroHiddenFlagAsHidden(byte hidden, bool expected)
    {
        var badge = new Badge(
            BadgeDtoBuilder.Valid().WithId(1).WithName("Un badge").WithDescription("en").WithHiddenFlag(hidden).Build(),
            12, null);

        badge.Hidden.Should().Be(expected);
        badge.Users.Should().Be(12);
    }

    [Fact]
    public void Badge_UsesTheTranslationWhenThereIsOne()
    {
        var dto = BadgeDtoBuilder.Valid().WithId(1).WithName("Un badge").WithDescription("english description").Build();

        var translated = new Badge(dto, 1, new BadgeTranslationDto { Name = "Un badge en français", Description = "description française" });
        translated.Name.Should().Be("Un badge en français");
        translated.Description.Should().Be("description française");

        var untranslated = new Badge(dto, 1, null);
        untranslated.Name.Should().Be("Un badge");
        untranslated.Description.Should().Be("english description");
    }

    [Fact]
    public void Badge_FallsBackPerFieldWhenATranslatedFieldIsBlank()
    {
        var dto = BadgeDtoBuilder.Valid().WithId(1).WithName("A badge").WithDescription("english description").Build();

        var blankName = new Badge(dto, 1, new BadgeTranslationDto { Name = " ", Description = "description française" });
        blankName.Name.Should().Be("A badge");
        blankName.Description.Should().Be("description française");

        var blankDescription = new Badge(dto, 1, new BadgeTranslationDto { Name = "Un badge", Description = "" });
        blankDescription.Name.Should().Be("Un badge");
        blankDescription.Description.Should().Be("english description");
    }

    [Fact]
    public void UserBadge_ForwardsTheBadgeAndStampsTheDate()
    {
        var badge = new Badge(
            BadgeDtoBuilder.Valid().WithId(5).WithName("Wooden spoon").WithDescription("d").WithHiddenFlag(1).Build(), 3, null);
        var date = new DateOnly(2026, 9, 2);

        var userBadge = new UserBadge(badge, date);

        userBadge.Id.Should().Be(5);
        userBadge.Name.Should().Be("Wooden spoon");
        userBadge.Users.Should().Be(3);
        userBadge.Hidden.Should().BeTrue();
        userBadge.GetDate.Should().Be(date);
    }

    // ------------------------------------------------------------- Player

    private static PlayerFullDto Submission(ulong creatorId)
    {
        return new PlayerFullDto
        {
            Player = PlayerDtoBuilder.Valid().WithId(1).WithName("Zinédine Zidane").WithAllowedNames("zidane;zizou").WithYearOfBirth(1972).WithCountryId((ulong)Countries.FRA).WithPositionId((ulong)Positions.Midfielder).WithCreator(creatorId).WithClue("un indice").WithEasyClue("un indice facile").Build(),
            Clubs = new List<ClubDto> { ClubDtoBuilder.Valid().WithId(2).WithName("Real Madrid").Build() },
            PlayerClubs = new List<PlayerClubDto>
            {
                new() { PlayerId = 1, ClubId = 2, HistoryPosition = 4 }
            }
        };
    }

    [Fact]
    public void Player_MapsEveryIdentifierOntoItsEnum()
    {
        var users = new List<UserDto> { UserDtoBuilder.Valid().WithId(42).WithLogin("createur").Build() };

        var player = new Player(Submission(42), users, TestCountryContinents.Map);

        player.Id.Should().Be(1);
        player.Country.Should().Be(Countries.FRA);
        player.Continent.Should().Be(Continents.Europe);
        player.Position.Should().Be(Positions.Midfielder);
        player.YearOfBirth.Should().Be(1972);
        player.Clubs.Should().ContainSingle().Which.Name.Should().Be("Real Madrid");
    }

    [Fact]
    public void Player_AlwaysExposesTheAnswerToTheAdministrationScreen()
    {
        // ce constructeur sert l'ecran de validation des soumissions : contrairement
        // a PlayerCreator, il ne masque rien
        var users = new List<UserDto> { UserDtoBuilder.Valid().WithId(42).WithLogin("createur").Build() };

        var player = new Player(Submission(42), users, TestCountryContinents.Map);

        player.Name.Should().Be("Zinédine Zidane");
        player.AllowedNames.Should().BeEquivalentTo(new[] { "zidane", "zizou" });
        player.Login.Should().Be("createur");
    }

    [Fact]
    public void Player_WhenTheCreatorIsAbsentFromTheList_SaysWhichCreatorIsMissing()
    {
        Action act = () => new Player(Submission(99), new List<UserDto>(), TestCountryContinents.Map);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*99*");
    }
}

/// <summary>
/// Horloge applicative. Les proprietes derivent toutes de l'heure systeme : on
/// verifie leurs relations entre elles, pas leur valeur absolue.
/// </summary>
public class ClockTests
{
    private readonly IClock _clock = new Clock();

    [Fact]
    public void TomorrowAndYesterdayFrameToday()
    {
        _clock.Tomorrow.Should().Be(_clock.Today.AddDays(1));
        _clock.Yesterday.Should().Be(_clock.Today.AddDays(-1));
    }

    [Fact]
    public void TomorrowEndIsTheLastSecondOfTomorrow()
    {
        _clock.TomorrowEnd.Should().Be(_clock.Tomorrow.AddDays(1).ToDateTime(TimeOnly.MinValue).AddSeconds(-1));
        _clock.TomorrowEnd.TimeOfDay.Should().Be(new TimeSpan(23, 59, 59));
    }

    [Fact]
    public void FirstOfMonthIsTheFirstDayOfTheCurrentMonth()
    {
        _clock.FirstOfMonth.Day.Should().Be(1);
        _clock.FirstOfMonth.Month.Should().Be(_clock.Today.Month);
        _clock.FirstOfMonth.Year.Should().Be(_clock.Today.Year);
    }

    [Fact]
    public void NowSecondsDropsTheMilliseconds()
    {
        _clock.NowSeconds.Millisecond.Should().Be(0);
    }

    [Fact]
    public void IsTomorrowIn_IsFalseForZeroMinutesAndTrueForAWholeDay()
    {
        // garde-fou de ReassignPlayersOfTheDayAsync : ajouter 24 h franchit
        // forcement minuit, ajouter zero minute ne le franchit jamais
        _clock.IsTomorrowIn(0).Should().BeFalse();
        _clock.IsTomorrowIn(24 * 60).Should().BeTrue();
    }

    [Fact]
    public void IsTomorrowIn_IsMonotonic()
    {
        // si un delai court franchit minuit, un delai plus long le franchit aussi
        if (_clock.IsTomorrowIn(30))
            _clock.IsTomorrowIn(60).Should().BeTrue();
    }
}
