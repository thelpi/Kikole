using System;
using System.Linq;
using FluentAssertions;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using Xunit;

namespace KikoleSiteUnitTests.Models;

public class ProposalTypeExtensionsTests
{
    [Theory]
    [InlineData(ProposalTypes.Name)]
    [InlineData(ProposalTypes.Club)]
    [InlineData(ProposalTypes.Year)]
    [InlineData(ProposalTypes.Country)]
    [InlineData(ProposalTypes.Continent)]
    [InlineData(ProposalTypes.Position)]
    public void CanBeMiss_GuessableTypes_ReturnTrue(ProposalTypes type)
    {
        type.CanBeMiss().Should().BeTrue();
    }

    [Theory]
    [InlineData(ProposalTypes.Clue)]
    [InlineData(ProposalTypes.Leaderboard)]
    public void CanBeMiss_PurchasedTypes_ReturnFalse(ProposalTypes type)
    {
        // ces deux types ne devinent rien : ils sont toujours "reussis", et c'est
        // precisement CanBeMiss qui les fait quand meme couter des points
        type.CanBeMiss().Should().BeFalse();
    }

    [Fact]
    public void ProposalTypesCost_CoversEveryProposalType()
    {
        // garde-fou : ProposalResponse indexe le dictionnaire sans verification,
        // un type ajoute a l'enum sans son cout leverait une KeyNotFoundException
        var allTypes = Enum.GetValues(typeof(ProposalTypes)).Cast<ProposalTypes>();

        allTypes.Should().OnlyContain(t => ProposalChart.ProposalTypesCost.ContainsKey(t));
    }

    [Fact]
    public void ProposalTypesCost_OnlyTheClueIsExpressedAsARate()
    {
        var rateBased = ProposalChart.ProposalTypesCost
            .Where(kvp => kvp.Value.isRate)
            .Select(kvp => kvp.Key);

        rateBased.Should().Equal(ProposalTypes.Clue);
    }

    [Fact]
    public void ProposalTypesCost_EveryCostIsPositive()
    {
        ProposalChart.ProposalTypesCost.Values.Should().OnlyContain(v => v.points > 0);
    }

}
