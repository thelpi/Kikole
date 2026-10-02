using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KikoleSite.Helpers;
using Xunit;

namespace KikoleSiteUnitTests.Helpers;

public class AssignmentHelperTests
{
    private static int Max(
        IEnumerable<string[]> items,
        IReadOnlyDictionary<string, int> capacities)
        => AssignmentHelper.MaxAssignments(items, i => i, capacities);

    private static Dictionary<string, int> Slots(params (string slot, int capacity)[] slots)
        => slots.ToDictionary(s => s.slot, s => s.capacity);

    [Fact]
    public void NoItemMeansNoAssignment()
    {
        Max([], Slots(("A", 1))).Should().Be(0);
    }

    [Fact]
    public void AnItemFillsOneSlotOnly_EvenWhenEligibleForTwo()
    {
        // un seul joueur "A ou B" : il ne peut pas remplir A et B a la fois
        Max([["A", "B"]], Slots(("A", 1), ("B", 1))).Should().Be(1);
    }

    [Fact]
    public void CapacityBoundsTheNumberOfItemsPerSlot()
    {
        Max([["A"], ["A"], ["A"]], Slots(("A", 2))).Should().Be(2);
    }

    [Fact]
    public void AnItemWithNoEligibleSlotIsNeverAssigned()
    {
        Max([["Z"], ["A"]], Slots(("A", 1))).Should().Be(1);
    }

    [Fact]
    public void AnOccupantIsMovedElsewhereToMakeRoom()
    {
        // un glouton affecterait le 1er joueur a A et bloquerait le 2e (A seul) ; le bon
        // algorithme deplace le 1er sur B
        Max([["A", "B"], ["A"]], Slots(("A", 1), ("B", 1))).Should().Be(2);
    }

    [Fact]
    public void LongReroutingChainsAreResolved()
    {
        // chaine : 1 -> {A,B}, 2 -> {B,C}, 3 -> {C,D}, 4 -> {A} : tout le monde doit
        // pouvoir etre place en decalant les autres
        Max([["A", "B"], ["B", "C"], ["C", "D"], ["A"]], Slots(("A", 1), ("B", 1), ("C", 1), ("D", 1)))
            .Should().Be(4);
    }

    [Fact]
    public void ImpossibleDemandIsDetected()
    {
        // trois joueurs, deux places distinctes possibles pour eux
        Max([["A", "B"], ["A", "B"], ["A", "B"]], Slots(("A", 1), ("B", 1))).Should().Be(2);
    }

    [Fact]
    public void ASlotMissingFromTheCapacitiesDoesNotExist()
    {
        Max([["A", "Z"]], Slots(("Z", 0))).Should().Be(0);
    }
}
