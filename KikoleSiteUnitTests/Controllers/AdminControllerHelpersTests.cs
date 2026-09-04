using System.Collections.Generic;
using FluentAssertions;
using KikoleSite.Controllers;
using KikoleSite.Models;
using KikoleSite.Models.Requests;
using Xunit;

namespace KikoleSiteUnitTests.Controllers;

/// <summary>
/// Deux methodes privees d'<see cref="AdminController"/> portent de la vraie logique de
/// parsing/validation, jamais caracterisee jusqu'ici faute de contrôleur testable : passees
/// en <c>internal</c> (comme <c>ProposalRequest.GetTip</c> ou <c>ScoreCalculator</c>) pour
/// les exposer au projet de tests, sans changement de comportement.
/// </summary>
public class AdminControllerHelpersTests
{
    // ------------------------------------------------------------- SplitAlternativeNames

    [Fact]
    public void SplitAlternativeNames_PutsTheCanonicalNameFirst()
    {
        var result = AdminController.SplitAlternativeNames("Zinédine Zidane", "zidane\nzizou");

        result.Should().ContainInOrder("Zinédine Zidane", "zidane", "zizou");
    }

    [Fact]
    public void SplitAlternativeNames_WithNoAlias_ReturnsOnlyTheCanonicalName()
    {
        var result = AdminController.SplitAlternativeNames("Zinédine Zidane", null);

        result.Should().ContainSingle().Which.Should().Be("Zinédine Zidane");
    }

    [Fact]
    public void SplitAlternativeNames_IgnoresEmptyLinesAndSurroundingSpaces()
    {
        var result = AdminController.SplitAlternativeNames("Pelé", "\n  Edson  \n\n  \nArantes\n");

        result.Should().ContainInOrder("Pelé", "Edson", "Arantes");
    }

    [Fact]
    public void SplitAlternativeNames_DropsDuplicatesOfTheCanonicalNameCaseInsensitively()
    {
        var result = AdminController.SplitAlternativeNames("Pelé", "PELÉ\nEdson");

        result.Should().ContainInOrder("Pelé", "Edson");
    }

    [Fact]
    public void SplitAlternativeNames_DropsExactDuplicateAliases()
    {
        var result = AdminController.SplitAlternativeNames("Pelé", "Edson\nedson\nEdson");

        result.Should().ContainInOrder("Pelé", "Edson", "edson");
    }

    // ------------------------------------------------------------- AddClubIfValid

    private static readonly IReadOnlyCollection<Club> Referential =
    [
        new Club(ClubDtoBuilder.Valid().WithId(10).WithName("Real Madrid").Build(), []),
        new Club(ClubDtoBuilder.Valid().WithId(11).WithName("Juventus").Build(), []),
    ];

    [Fact]
    public void AddClubIfValid_WithAKnownId_AppendsItAndAdvancesTheHistoryPosition()
    {
        var clubs = new List<PlayerClubRequest>();
        byte i = 0;

        AdminController.AddClubIfValid(clubs, "10", Referential, ref i, isLoan: false);

        clubs.Should().ContainSingle();
        clubs[0].ClubId.Should().Be(10);
        clubs[0].HistoryPosition.Should().Be(0);
        clubs[0].IsLoan.Should().BeFalse();
        i.Should().Be(1);
    }

    [Fact]
    public void AddClubIfValid_CalledSeveralTimes_KeepsIncrementingThePositionOnlyOnSuccess()
    {
        var clubs = new List<PlayerClubRequest>();
        byte i = 0;

        AdminController.AddClubIfValid(clubs, "10", Referential, ref i, isLoan: false);
        AdminController.AddClubIfValid(clubs, "", Referential, ref i, isLoan: false); // ignoré, i inchangé
        AdminController.AddClubIfValid(clubs, "11", Referential, ref i, isLoan: true);

        clubs.Should().HaveCount(2);
        clubs[0].HistoryPosition.Should().Be(0);
        clubs[1].HistoryPosition.Should().Be(1);
        clubs[1].IsLoan.Should().BeTrue();
        i.Should().Be(2);
    }

    [Fact]
    public void AddClubIfValid_WithAnIdOutsideTheReferential_IsIgnored()
    {
        var clubs = new List<PlayerClubRequest>();
        byte i = 0;

        AdminController.AddClubIfValid(clubs, "999", Referential, ref i, isLoan: false);

        clubs.Should().BeEmpty();
        i.Should().Be(0);
    }

    [Fact]
    public void AddClubIfValid_WithANonNumericValue_IsIgnored()
    {
        var clubs = new List<PlayerClubRequest>();
        byte i = 0;

        AdminController.AddClubIfValid(clubs, "pas-un-id", Referential, ref i, isLoan: false);

        clubs.Should().BeEmpty();
        i.Should().Be(0);
    }

    [Fact]
    public void AddClubIfValid_WithANullValue_IsIgnored()
    {
        var clubs = new List<PlayerClubRequest>();
        byte i = 0;

        AdminController.AddClubIfValid(clubs, null, Referential, ref i, isLoan: false);

        clubs.Should().BeEmpty();
        i.Should().Be(0);
    }
}
