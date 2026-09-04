using System;
using System.Collections.Generic;
using FluentAssertions;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Models;

public class ProposalResponseTests
{
    private const ulong RealMadridId = 10;
    private const ulong JuventusId = 11;

    private readonly IStringLocalizer _localizer;

    public ProposalResponseTests()
    {
        var mock = new Mock<IStringLocalizer>();
        mock.Setup(_ => _[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));
        _localizer = mock.Object;
    }

    private static PlayerFullDto Zidane()
    {
        return new PlayerFullDto
        {
            Player = PlayerDtoBuilder.Valid().WithId(1).WithName("Zinédine Zidane").WithAllowedNames("zidane;zizou;zinedine zidane").WithYearOfBirth(1972).WithCountryId((ulong)Countries.FRA).WithPositionId((ulong)Positions.Midfielder).Build(),
            Clubs = new List<ClubDto>
            {
                ClubDtoBuilder.Valid().WithId(RealMadridId).WithName("Real Madrid").Build(),
                ClubDtoBuilder.Valid().WithId(JuventusId).WithName("Juventus").Build()
            },
            PlayerClubs = new List<PlayerClubDto>
            {
                new() { PlayerId = 1, ClubId = JuventusId, HistoryPosition = 3 },
                new() { PlayerId = 1, ClubId = RealMadridId, HistoryPosition = 4 }
            }
        };
    }

    private static PlayerFullDto Sammer()
    {
        return new PlayerFullDto
        {
            Player = PlayerDtoBuilder.Valid().WithId(2).WithName("Matthias Sammer").WithAllowedNames("sammer;matthias sammer").WithYearOfBirth(1967).WithCountryId((ulong)Countries.GDR).WithAlternativeCountryId((ulong)Countries.GER).WithPositionId((ulong)Positions.Defender).Build(),
            Clubs = [],
            PlayerClubs = []
        };
    }

    /// <summary>Joueur fictif au parcours international double sur deux continents (type Algerie/France).</summary>
    private static PlayerFullDto DualContinentPlayer()
    {
        return new PlayerFullDto
        {
            Player = PlayerDtoBuilder.Valid().WithId(3).WithName("Joueur Double Continent").WithAllowedNames("double").WithYearOfBirth(1990).WithCountryId((ulong)Countries.BRA).WithAlternativeCountryId((ulong)Countries.FRA).WithPositionId((ulong)Positions.Forward).Build(),
            Clubs = [],
            PlayerClubs = []
        };
    }

    /// <summary>Joueur fictif occupant plausiblement deux postes (type Eden Hazard, milieu/attaquant).</summary>
    private static PlayerFullDto DualPositionPlayer()
    {
        return new PlayerFullDto
        {
            Player = PlayerDtoBuilder.Valid().WithId(4).WithName("Joueur Double Poste").WithAllowedNames("double poste").WithYearOfBirth(1991).WithCountryId((ulong)Countries.FRA).WithPositionId((ulong)Positions.Midfielder).WithAlternativePositionId((ulong)Positions.Forward).Build(),
            Clubs = [],
            PlayerClubs = []
        };
    }

    private ProposalResponse Respond(ProposalTypes type, string value, PlayerFullDto? player = null)
    {
        var request = new ProposalRequest
        {
            Value = value,
            ProposalType = type,
            ProposalDateTime = new DateTime(2026, 9, 2, 18, 0, 0)
        };

        return new ProposalResponse(request, player ?? Zidane(), _localizer, TestCountryContinents.Map);
    }

    // ------------------------------------------------------------- nom du joueur

    [Theory]
    [InlineData("Zidane")]
    [InlineData("zidane")]
    [InlineData("  Zidane  ")]
    [InlineData("Zinédine Zidane")]
    [InlineData("Zinedine Zidane")]  // sans accent
    [InlineData("Zidan")]            // faute toleree
    public void Name_WhenMatching_IsSuccessfulAndCostsNothing(string value)
    {
        var response = Respond(ProposalTypes.Name, value);

        response.Successful.Should().BeTrue();
        response.Cost.Should().Be((0, false));
        response.Value.Should().Be("Zinédine Zidane");
        response.IsWin.Should().BeTrue();
    }

    [Fact]
    public void Name_WhenWrong_CostsTheFullPenaltyAndEchoesTheInput()
    {
        var response = Respond(ProposalTypes.Name, "Ronaldo");

        response.Successful.Should().BeFalse();
        response.Cost.Should().Be((400, false));
        response.Value.Should().Be("Ronaldo");
        response.IsWin.Should().BeFalse();
    }

    [Fact]
    public void IsWin_IsOnlyTrueForASuccessfulNameProposal()
    {
        // un club correct ne gagne pas la partie
        Respond(ProposalTypes.Club, RealMadridId.ToString()).IsWin.Should().BeFalse();
    }

    // ------------------------------------------------------------- clubs
    // la proposition porte desormais l'identifiant du club (choisi via l'autocompletion),
    // plus un texte libre a sanitiser/tolerer : le match est exact par construction.

    [Fact]
    public void Club_WhenMatchingTheClubId_IsSuccessfulAndReturnsTheCareerEntries()
    {
        var response = Respond(ProposalTypes.Club, RealMadridId.ToString());

        response.Successful.Should().BeTrue();
        response.Cost.Should().Be((0, false));

        var clubs = response.Value.Should().BeOfType<List<PlayerClub>>().Subject;
        clubs.Should().HaveCount(1);
        clubs[0].Name.Should().Be("Real Madrid");
        clubs[0].HistoryPosition.Should().Be(4);
    }

    [Fact]
    public void Club_WhenTheIdIsNotInTheCareer_IsRejected()
    {
        // un identifiant de club valide, mais absent de la carriere du joueur
        var response = Respond(ProposalTypes.Club, "999");

        response.Successful.Should().BeFalse();
        response.Cost.Should().Be((50, false));
    }

    [Fact]
    public void Club_WhenWrong_CostsThePenaltyAndEchoesTheInput()
    {
        var response = Respond(ProposalTypes.Club, "999");

        response.Successful.Should().BeFalse();
        response.Value.Should().Be("999");
    }

    [Fact]
    public void Club_WhenTheValueIsNotANumericId_IsRejectedWithoutThrowing()
    {
        var response = Respond(ProposalTypes.Club, "Real Madrid");

        response.Successful.Should().BeFalse();
    }

    // ------------------------------------------------------------- nationalite / continent

    [Fact]
    public void Country_WhenMatching_IsSuccessful()
    {
        var response = Respond(ProposalTypes.Country, nameof(Countries.FRA));

        response.Successful.Should().BeTrue();
        response.Cost.Should().Be((0, false));
        response.Value.Should().Be((ulong)Countries.FRA);
    }

    [Fact]
    public void Country_WhenWrong_CostsThePenalty()
    {
        var response = Respond(ProposalTypes.Country, nameof(Countries.BRA));

        response.Successful.Should().BeFalse();
        response.Cost.Should().Be((25, false));
    }

    [Fact]
    public void Country_WhenMatchingTheMainOne_IsSuccessfulAndExposesTheAlternative()
    {
        var response = Respond(ProposalTypes.Country, nameof(Countries.GDR), Sammer());

        response.Successful.Should().BeTrue();
        response.Value.Should().Be((ulong)Countries.GDR);
        response.AlternativeCountryId.Should().Be((ulong)Countries.GER);
    }

    [Fact]
    public void Country_WhenMatchingTheAlternativeOne_IsAlsoSuccessful()
    {
        var response = Respond(ProposalTypes.Country, nameof(Countries.GER), Sammer());

        response.Successful.Should().BeTrue();
        response.AlternativeCountryId.Should().Be((ulong)Countries.GER);
    }

    [Fact]
    public void Country_WithAnAlternativeOnThePlayer_ButNeitherIsGuessed_IsUnsuccessful()
    {
        var response = Respond(ProposalTypes.Country, nameof(Countries.FRA), Sammer());

        response.Successful.Should().BeFalse();
        response.AlternativeCountryId.Should().BeNull();
    }

    [Fact]
    public void Continent_WhenMatching_IsSuccessful()
    {
        var response = Respond(ProposalTypes.Continent, nameof(Continents.Europe));

        response.Successful.Should().BeTrue();
        response.Value.Should().Be((ulong)Continents.Europe);
    }

    [Fact]
    public void Continent_WhenWrong_IsTheMostExpensiveNonNamePenalty()
    {
        var response = Respond(ProposalTypes.Continent, nameof(Continents.Africa));

        response.Successful.Should().BeFalse();
        response.Cost.Should().Be((100, false));
    }

    [Fact]
    public void Continent_IsDeducedFromTheCountry_NotStoredIndependently()
    {
        // Zidane est France -> Europe : deviner Europe reussit sans continent stocke sur le joueur
        var response = Respond(ProposalTypes.Continent, nameof(Continents.Europe));

        response.Successful.Should().BeTrue();
        response.Value.Should().Be((ulong)Continents.Europe);
    }

    [Fact]
    public void Continent_WithCountryAndAlternativeOnDifferentContinents_EitherIsAccepted()
    {
        // pays principal Bresil (Amerique du Sud), alternatif France (Europe)
        var southAmerica = Respond(ProposalTypes.Continent, nameof(Continents.SouthAmerica), DualContinentPlayer());
        var europe = Respond(ProposalTypes.Continent, nameof(Continents.Europe), DualContinentPlayer());

        southAmerica.Successful.Should().BeTrue();
        europe.Successful.Should().BeTrue();
    }

    [Fact]
    public void Continent_WithCountryAndAlternativeOnDifferentContinents_ExposesBothOnSuccess()
    {
        var response = Respond(ProposalTypes.Continent, nameof(Continents.SouthAmerica), DualContinentPlayer());

        response.AlternativeContinentId.Should().Be((ulong)Continents.Europe);
    }

    // ------------------------------------------------------------- poste / annee

    [Fact]
    public void Position_IsMatchedByNumericValueNotByName()
    {
        // asymetrie avec le pays et le continent, qui attendent le nom de l'enum
        var response = Respond(ProposalTypes.Position, ((int)Positions.Midfielder).ToString());

        response.Successful.Should().BeTrue();
        response.RawValue.Should().Be(nameof(Positions.Midfielder));
    }

    [Fact]
    public void Position_WhenWrong_CostsThePenalty()
    {
        var response = Respond(ProposalTypes.Position, ((int)Positions.Goalkeeper).ToString());

        response.Successful.Should().BeFalse();
        response.Cost.Should().Be((75, false));
    }

    [Fact]
    public void Position_WhenMatchingTheMainOne_IsSuccessfulAndExposesTheAlternative()
    {
        var response = Respond(ProposalTypes.Position, ((int)Positions.Midfielder).ToString(), DualPositionPlayer());

        response.Successful.Should().BeTrue();
        response.Value.Should().Be((ulong)Positions.Midfielder);
        response.AlternativePositionId.Should().Be((ulong)Positions.Forward);
    }

    [Fact]
    public void Position_WhenMatchingTheAlternativeOne_IsAlsoSuccessful()
    {
        var response = Respond(ProposalTypes.Position, ((int)Positions.Forward).ToString(), DualPositionPlayer());

        response.Successful.Should().BeTrue();
        response.AlternativePositionId.Should().Be((ulong)Positions.Forward);
    }

    [Fact]
    public void Position_WithAnAlternativeOnThePlayer_ButNeitherIsGuessed_IsUnsuccessful()
    {
        var response = Respond(ProposalTypes.Position, ((int)Positions.Goalkeeper).ToString(), DualPositionPlayer());

        response.Successful.Should().BeFalse();
        response.AlternativePositionId.Should().BeNull();
    }

    [Fact]
    public void Year_WhenMatching_IsSuccessful()
    {
        var response = Respond(ProposalTypes.Year, "1972");

        response.Successful.Should().BeTrue();
        response.Value.Should().Be((ushort)1972);
    }

    [Theory]
    // l'indice porte sur le joueur, pas sur la proposition : une annee trop tardive
    // signifie que le joueur est plus vieux que ce qu'on a propose
    [InlineData("1980", "TipOlderPlayer")]
    [InlineData("1960", "TipYoungerPlayer")]
    public void Year_WhenWrong_GivesADirectionalHint(string value, string expectedTip)
    {
        var response = Respond(ProposalTypes.Year, value);

        response.Successful.Should().BeFalse();
        response.Cost.Should().Be((25, false));
        response.Tip.Should().Be(expectedTip);
    }

    // ------------------------------------------------------------- achats (indice, classement)

    [Fact]
    public void Clue_AlwaysSucceedsButStillCosts_AsAPercentage()
    {
        var response = Respond(ProposalTypes.Clue, "GetClue");

        response.Successful.Should().BeTrue();
        response.Cost.Should().Be((50, true));  // true = taux
        response.Value.Should().BeNull();
        response.RawValue.Should().BeEmpty();
    }

    [Fact]
    public void Leaderboard_AlwaysSucceedsButStillCosts_AsAFlatAmount()
    {
        var response = Respond(ProposalTypes.Leaderboard, "GetLeaderboard");

        response.Successful.Should().BeTrue();
        response.Cost.Should().Be((25, false));
    }

    // ------------------------------------------------------------- WithTotalPoints

    [Fact]
    public void WithTotalPoints_ASuccessfulGuessIsFree()
    {
        var response = Respond(ProposalTypes.Name, "Zidane").WithTotalPoints(1000, false);

        response.TotalPoints.Should().Be(1000);
        response.PointsLost.Should().Be(0);
    }

    [Fact]
    public void WithTotalPoints_AWrongGuessSubtractsAFlatAmount()
    {
        var response = Respond(ProposalTypes.Name, "Ronaldo").WithTotalPoints(1000, false);

        response.TotalPoints.Should().Be(600);
        response.PointsLost.Should().Be(400);
    }

    [Fact]
    public void WithTotalPoints_TheClueHalvesTheRemainingScore()
    {
        // le taux s'applique au score courant, pas au score de depart :
        // deux indices coutent 500 puis 250, pas 500 puis 500
        var first = Respond(ProposalTypes.Clue, "x").WithTotalPoints(1000, false);
        first.TotalPoints.Should().Be(500);
        first.PointsLost.Should().Be(500);

        var second = Respond(ProposalTypes.Clue, "x").WithTotalPoints(first.TotalPoints, false);
        second.TotalPoints.Should().Be(250);
        second.PointsLost.Should().Be(250);
    }

    [Fact]
    public void WithTotalPoints_NeverGoesBelowZero()
    {
        var response = Respond(ProposalTypes.Name, "Ronaldo").WithTotalPoints(100, false);

        response.TotalPoints.Should().Be(0);
        // le tarif de la categorie est 400, mais il ne restait que 100 a perdre : PointsLost
        // reflete la perte reelle (plafonnee), pas le tarif brut de Cost
        response.PointsLost.Should().Be(100);
    }

    [Fact]
    public void WithTotalPoints_ADuplicateProposalIsNotChargedTwice()
    {
        var response = Respond(ProposalTypes.Name, "Ronaldo").WithTotalPoints(1000, true);

        response.TotalPoints.Should().Be(1000);
        response.PointsLost.Should().Be(0);
    }

    // ------------------------------------------------------------- badges

    [Fact]
    public void CollectedBadges_StartsEmptyAndAccumulates()
    {
        var response = Respond(ProposalTypes.Name, "Zidane");
        response.CollectedBadges.Should().BeEmpty();

        var badge = new Badge(
            BadgeDtoBuilder.Valid().WithId(1).WithName("Your first success").WithDescription("d").Build(), 1, null);
        response.AddBadge(new UserBadge(badge, new DateTime(2026, 9, 2)));

        response.CollectedBadges.Should().HaveCount(1);
    }
}
