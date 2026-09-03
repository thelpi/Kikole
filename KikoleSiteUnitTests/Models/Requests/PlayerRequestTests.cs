using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Models.Requests;

public class PlayerRequestTests
{
    private static readonly DateTime Today = new DateTime(2026, 9, 2);

    private readonly IStringLocalizer _localizer;

    public PlayerRequestTests()
    {
        var mock = new Mock<IStringLocalizer>();
        mock.Setup(_ => _[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));
        _localizer = mock.Object;
    }

    private static PlayerRequest Valid()
    {
        return new PlayerRequest
        {
            Name = "Zinédine Zidane",
            YearOfBirth = 1972,
            Country = Countries.FR,
            Continent = Continents.Europe,
            Position = Positions.Midfielder,
            AllowedNames = new List<string> { "Zidane", "Zizou" },
            Clubs = new List<PlayerClubRequest>
            {
                new PlayerClubRequest { ClubId = 1, HistoryPosition = 1 },
                new PlayerClubRequest { ClubId = 2, HistoryPosition = 2 },
                new PlayerClubRequest { ClubId = 3, HistoryPosition = 3 }
            },
            ClueEn = "A clue",
            EasyClueEn = "An easier clue",
            ClueLanguages = new Dictionary<Languages, string?>(),
            EasyClueLanguages = new Dictionary<Languages, string?>()
        };
    }

    private static List<PlayerClubRequest> Career(params byte[] positions)
    {
        return positions
            .Select((p, i) => new PlayerClubRequest { ClubId = (ulong)(i + 1), HistoryPosition = p })
            .ToList();
    }

    // ------------------------------------------------------------- cas nominal

    [Fact]
    public void IsValid_WhenEverythingIsFilled_ReturnsNull()
    {
        Valid().IsValid(Today, _localizer).Should().BeNull();
    }

    // ------------------------------------------------------------- nom

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_WhenNameIsBlank_IsRejected(string name)
    {
        var request = Valid() with { Name = name };

        request.IsValid(Today, _localizer).Should().Be("InvalidName");
    }

    // ------------------------------------------------------------- annee de naissance

    [Theory]
    [InlineData(1849, false)]
    [InlineData(1850, true)]   // borne basse incluse
    [InlineData(1972, true)]
    [InlineData(2100, true)]   // borne haute incluse
    [InlineData(2101, false)]
    [InlineData(0, false)]
    public void IsValid_ChecksBirthYearBounds(ushort year, bool expectedValid)
    {
        var request = Valid() with { YearOfBirth = year };

        var result = request.IsValid(Today, _localizer);

        if (expectedValid)
            result.Should().BeNull();
        else
            result.Should().Be("InvalidBirthYear");
    }

    // ------------------------------------------------------------- noms alternatifs

    [Fact]
    public void IsValid_WhenAllowedNamesIsEmpty_IsRejected()
    {
        var request = Valid() with { AllowedNames = [] };

        request.IsValid(Today, _localizer).Should().Be("InvalidAllowedNames");
    }

    [Fact]
    public void IsValid_WhenAnyAllowedNameIsBlank_IsRejected()
    {
        var request = Valid() with { AllowedNames = new List<string> { "Zidane", "  " } };

        request.IsValid(Today, _localizer).Should().Be("InvalidAllowedNames");
    }

    // ------------------------------------------------------------- carriere

    [Fact]
    public void IsValid_WhenCareerIsEmpty_IsRejected()
    {
        var request = Valid() with { Clubs = [] };

        request.IsValid(Today, _localizer).Should().Be("EmptyClubsList");
    }

    [Fact]
    public void IsValid_WhenAClubHasNoIdentifier_IsRejected()
    {
        var request = Valid() with { Clubs = new List<PlayerClubRequest>
        {
            new PlayerClubRequest { ClubId = 0, HistoryPosition = 1 }
        } };

        request.IsValid(Today, _localizer).Should().Be("InvalidClubs");
    }

    [Theory]
    // la carriere doit numeroter 1..N sans trou ni doublon
    [InlineData(new byte[] { 1 }, true)]
    [InlineData(new byte[] { 1, 2, 3 }, true)]
    [InlineData(new byte[] { 3, 1, 2 }, true)]     // l'ordre de saisie est libre
    [InlineData(new byte[] { 1, 2, 4 }, false)]    // trou
    [InlineData(new byte[] { 1, 2, 2 }, false)]    // doublon
    [InlineData(new byte[] { 2, 3, 4 }, false)]    // ne commence pas a 1
    [InlineData(new byte[] { 0, 1, 2 }, false)]    // commence a 0
    public void IsValid_CareerPositionsMustBeContiguousFromOne(byte[] positions, bool expectedValid)
    {
        var request = Valid() with { Clubs = Career(positions) };

        var result = request.IsValid(Today, _localizer);

        if (expectedValid)
            result.Should().BeNull();
        else
            result.Should().Be("InvalidClubs");
    }

    // ------------------------------------------------------------- indices

    [Theory]
    [InlineData("", "easy")]
    [InlineData("   ", "easy")]
    [InlineData("clue", "")]
    [InlineData("clue", "   ")]
    public void IsValid_WhenAnEnglishClueIsMissing_IsRejected(string clue, string easyClue)
    {
        var request = Valid() with { ClueEn = clue, EasyClueEn = easyClue };

        request.IsValid(Today, _localizer).Should().Be("InvalidClue");
    }

    // ------------------------------------------------------------- date de parution

    [Fact]
    public void IsValid_WhenProposalDateIsInThePast_IsRejected()
    {
        var request = Valid() with { ProposalDate = Today.AddDays(-1) };

        request.IsValid(Today, _localizer).Should().Be("InvalidProposalDate");
    }

    [Theory]
    [InlineData(0)]   // aujourd'hui accepte
    [InlineData(1)]
    [InlineData(30)]
    public void IsValid_WhenProposalDateIsTodayOrLater_IsAccepted(int daysAhead)
    {
        var request = Valid() with { ProposalDate = Today.AddDays(daysAhead) };

        request.IsValid(Today, _localizer).Should().BeNull();
    }

    [Fact]
    public void IsValid_WhenProposalDateIsNotSet_IsAccepted()
    {
        // une soumission en attente de validation n'a pas encore de date
        var request = Valid() with { ProposalDate = null };

        request.IsValid(Today, _localizer).Should().BeNull();
    }

    [Fact]
    public void IsValid_IgnoresTheTimeOfDayOnTheProposalDate()
    {
        var request = Valid() with { ProposalDate = Today.AddHours(3) };

        request.IsValid(Today, _localizer).Should().BeNull();
    }

    // ------------------------------------------------------------- ToDto

    [Fact]
    public void ToDto_SanitizesAllowedNamesAndAppendsTheDisplayName()
    {
        var dto = Valid().ToDto(42, null);

        dto.AllowedNames.Should().Be("zidane;zizou;zinedine zidane");
        dto.Name.Should().Be("Zinédine Zidane");
    }

    [Fact]
    public void ToDto_MapsEnumsToTheirNumericIdentifiers()
    {
        var dto = Valid().ToDto(42, null);

        dto.CountryId.Should().Be((ulong)Countries.FR);
        dto.ContinentId.Should().Be((ulong)Continents.Europe);
        dto.PositionId.Should().Be((ulong)Positions.Midfielder);
        dto.CreationUserId.Should().Be(42);
    }

    [Theory]
    [InlineData(true, (byte)1)]
    [InlineData(false, (byte)0)]
    public void ToDto_ConvertsHideCreatorToAFlag(bool hide, byte expected)
    {
        var request = Valid() with { HideCreator = hide };

        request.ToDto(42, null).HideCreator.Should().Be(expected);
    }

    [Fact]
    public void ToPlayerClubDtos_StampsThePlayerIdOnEveryEntry()
    {
        var dtos = Valid().ToPlayerClubDtos(7);

        dtos.Should().HaveCount(3);
        dtos.Should().OnlyContain(d => d.PlayerId == 7);
        dtos.Select(d => d.HistoryPosition).Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
    }
}
