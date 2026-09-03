using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.ViewModels;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.ViewModels;

/// <summary>
/// Machine a etats de l'ecran de jeu : accumulation des propositions ratees et
/// revelation progressive des informations trouvees.
/// </summary>
public class HomeModelTests
{
    private const ulong JuventusId = 11;
    private const ulong RealMadridId = 10;

    private static readonly IReadOnlyDictionary<ulong, string> Countries =
        new Dictionary<ulong, string>
        {
            { (ulong)KikoleSite.Models.Enums.Countries.FR, "France" },
            { (ulong)KikoleSite.Models.Enums.Countries.BR, "Brésil" }
        };

    private static readonly IReadOnlyDictionary<ulong, string> Continents =
        new Dictionary<ulong, string>
        {
            { (ulong)KikoleSite.Models.Enums.Continents.Europe, "Europe" },
            { (ulong)KikoleSite.Models.Enums.Continents.Africa, "Afrique" }
        };

    private static readonly IReadOnlyDictionary<ulong, string> PositionNames =
        new Dictionary<ulong, string>
        {
            { (ulong)Positions.Goalkeeper, "Gardien de but" },
            { (ulong)Positions.Midfielder, "Milieu de terrain" }
        };

    private readonly IStringLocalizer _localizer;

    public HomeModelTests()
    {
        var mock = new Mock<IStringLocalizer>();
        mock.Setup(_ => _[It.IsAny<string>()])
            .Returns<string>(k => new LocalizedString(k, k));
        _localizer = mock.Object;
    }

    private static PlayerFullDto Zidane()
    {
        return new PlayerFullDto
        {
            Player = PlayerDtoBuilder.Valid().WithId(1).WithName("Zinédine Zidane").WithAllowedNames("zidane;zizou").WithYearOfBirth(1972).WithCountryId((ulong)KikoleSite.Models.Enums.Countries.FR).WithContinentId((ulong)KikoleSite.Models.Enums.Continents.Europe).WithPositionId((ulong)Positions.Midfielder).Build(),
            Clubs = new List<ClubDto>
            {
                ClubDtoBuilder.Valid().WithId(JuventusId).WithName("Juventus").WithAllowedNames("juve;juventus").Build(),
                ClubDtoBuilder.Valid().WithId(RealMadridId).WithName("Real Madrid").WithAllowedNames("real;real madrid").Build()
            },
            PlayerClubs = new List<PlayerClubDto>
            {
                new() { PlayerId = 1, ClubId = JuventusId, HistoryPosition = 3 },
                new() { PlayerId = 1, ClubId = RealMadridId, HistoryPosition = 4 }
            }
        };
    }

    private ProposalResponse Respond(ProposalTypes type, string value, int points = 1000)
    {
        var request = new ProposalRequest
        {
            Value = value,
            ProposalType = type,
            ProposalDateTime = new DateTime(2026, 9, 2, 18, 0, 0)
        };

        return new ProposalResponse(request, Zidane(), _localizer).WithTotalPoints(points, false);
    }

    private void Apply(HomeModel model, ProposalTypes type, string value, int sourcePoints = 1000, string? easyClue = null)
    {
        model.SetPropertiesFromProposal(
            Respond(type, value, sourcePoints), Countries, Continents, PositionNames, easyClue);
    }

    // ------------------------------------------------------------- navigation

    [Theory]
    [InlineData(0, -1, 1)]
    [InlineData(3, 2, 4)]
    public void DayNavigationCountsBackwards(int current, int next, int previous)
    {
        // CurrentDay est un nombre de jours AVANT aujourd'hui : le jour "suivant"
        // est donc celui dont l'ecart est plus petit
        var model = new HomeModel { CurrentDay = current };

        model.NextDay.Should().Be(next);
        model.PreviousDay.Should().Be(previous);
    }

    [Fact]
    public void DateOfDayGoesBackFromToday()
    {
        var model = new HomeModel
        {
            CurrentDate = new DateTime(2026, 9, 10),
            CurrentDay = 3
        };

        model.DateOfDay.Should().Be(new DateTime(2026, 9, 7));
    }

    [Fact]
    public void SetFinalFormIsUserIsCreator_RevealsTheAnswerAndFlagsTheCreator()
    {
        var model = new HomeModel();

        model.SetFinalFormIsUserIsCreator("Zinédine Zidane", new[] { "zidane", "zizou" });

        model.PlayerName.Should().Be("Zinédine Zidane");
        model.PlayerAllowedNames.Should().Be("zidane, zizou");
        model.IsCreator.Should().BeTrue();
    }

    // ------------------------------------------------------------- achats

    [Fact]
    public void BuyingTheLeaderboardOnlyFlipsItsFlag()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Leaderboard, "GetLeaderboard");

        model.LeaderboardAvailable.Should().BeTrue();
        model.Points.Should().Be(975);
        model.EasyClue.Should().BeNull();
    }

    [Fact]
    public void BuyingAClueExposesTheEasyClue()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Clue, "GetClue", easyClue: "un indice facile");

        model.EasyClue.Should().Be("un indice facile");
        // l'indice coute 50 % du score courant : 1000 -> 500
        model.Points.Should().Be(500);
    }

    // ------------------------------------------------------------- clubs

    [Fact]
    public void ACorrectClubIsAddedToTheKnownCareer()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Club, "Real Madrid");

        model.KnownPlayerClubs.Should().ContainSingle();
        model.KnownPlayerClubs[0].Name.Should().Be("Real Madrid");
        model.KnownPlayerClubs[0].HistoryPosition.Should().Be(4);
    }

    [Fact]
    public void TheCareerStaysSortedByHistoryPosition()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Club, "Real Madrid");   // position 4
        Apply(model, ProposalTypes.Club, "Juventus");      // position 3

        model.KnownPlayerClubs.Select(c => c.HistoryPosition).Should().ContainInOrder(3, 4);
    }

    [Fact]
    public void ProposingTheSameClubTwiceDoesNotDuplicateTheEntry()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Club, "Real Madrid");
        Apply(model, ProposalTypes.Club, "real");

        model.KnownPlayerClubs.Should().ContainSingle();
    }

    [Fact]
    public void WrongClubsAccumulateInTheirOwnList()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Club, "Barcelone", sourcePoints: 950);
        Apply(model, ProposalTypes.Club, "Chelsea", sourcePoints: 900);

        model.IncorrectClubs.Should().BeEquivalentTo(new[] { "Barcelone", "Chelsea" });
        model.KnownPlayerClubs.Should().BeEmpty();
    }

    // ------------------------------------------------------------- nationalite, continent, poste

    [Fact]
    public void ACorrectCountryIsResolvedToItsDisplayName()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Country, ((int)KikoleSite.Models.Enums.Countries.FR).ToString());

        model.CountryName.Should().Be("France");
        model.IncorrectCountries.Should().BeEmpty();
    }

    [Fact]
    public void AWrongCountryIsListedByItsDisplayNameNotItsIdentifier()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Country, ((int)KikoleSite.Models.Enums.Countries.BR).ToString(), sourcePoints: 975);

        model.CountryName.Should().BeNull();
        model.IncorrectCountries.Should().ContainSingle().Which.Should().Be("Brésil");
    }

    [Fact]
    public void ACorrectContinentIsResolved()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Continent, ((int)KikoleSite.Models.Enums.Continents.Europe).ToString());

        model.ContinentName.Should().Be("Europe");
    }

    [Fact]
    public void AWrongContinentIsListedByItsDisplayName()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Continent, ((int)KikoleSite.Models.Enums.Continents.Africa).ToString(), sourcePoints: 900);

        model.IncorrectContinents.Should().ContainSingle().Which.Should().Be("Afrique");
    }

    [Fact]
    public void ACorrectPositionIsResolved()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Position, ((int)Positions.Midfielder).ToString());

        model.Position.Should().Be("Milieu de terrain");
    }

    [Fact]
    public void AWrongPositionIsListedByItsDisplayName()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Position, ((int)Positions.Goalkeeper).ToString(), sourcePoints: 925);

        model.IncorrectPositions.Should().ContainSingle().Which.Should().Be("Gardien de but");
    }

    // ------------------------------------------------------------- nom et annee

    [Fact]
    public void TheWinningNameIsRevealedInItsCanonicalForm()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Name, "zidane");

        model.PlayerName.Should().Be("Zinédine Zidane");
        model.IncorrectNames.Should().BeEmpty();
    }

    [Fact]
    public void WrongNamesAccumulateAsTyped()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Name, "Ronaldo", sourcePoints: 600);
        Apply(model, ProposalTypes.Name, "Pelé", sourcePoints: 200);

        model.IncorrectNames.Should().BeEquivalentTo(new[] { "Ronaldo", "Pelé" });
    }

    [Fact]
    public void ACorrectYearIsRevealed()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Year, "1972");

        model.BirthYear.Should().Be("1972");
    }

    [Fact]
    public void AWrongYearIsStoredWithItsDirectionalHint()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Year, "1980", sourcePoints: 975);

        model.IncorrectYears.Should().ContainSingle();
        model.IncorrectYears[0].Item1.Should().Be("1980");
        model.IncorrectYears[0].Item2.Should().Be("TipOlderPlayer");
    }

    // ------------------------------------------------------------- accumulation

    [Fact]
    public void EachProposalRefreshesTheRunningScore()
    {
        var model = new HomeModel();

        // nationalite ratee : 1000 - 25
        Apply(model, ProposalTypes.Country, ((int)KikoleSite.Models.Enums.Countries.BR).ToString());
        model.Points.Should().Be(975);

        // poste rate, applique au score courant : 975 - 75
        Apply(model, ProposalTypes.Position, ((int)Positions.Goalkeeper).ToString(), sourcePoints: 975);
        model.Points.Should().Be(900);
    }

    [Fact]
    public void TheListsAreIndependentFromOneAnother()
    {
        var model = new HomeModel();

        Apply(model, ProposalTypes.Club, "Barcelone", sourcePoints: 950);
        Apply(model, ProposalTypes.Name, "Ronaldo", sourcePoints: 550);
        Apply(model, ProposalTypes.Year, "1980", sourcePoints: 525);

        model.IncorrectClubs.Should().ContainSingle();
        model.IncorrectNames.Should().ContainSingle();
        model.IncorrectYears.Should().ContainSingle();
        model.IncorrectCountries.Should().BeEmpty();
        model.IncorrectContinents.Should().BeEmpty();
        model.IncorrectPositions.Should().BeEmpty();
    }
}
