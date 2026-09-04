using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KikoleSite.Helpers;
using Xunit;

namespace KikoleSiteUnitTests.Helpers;

public class CollectionHelperTests
{
    private class Entry
    {
        public required string Name { get; set; }
        public int Points { get; set; }
        public int Position { get; set; }
    }

    private static List<Entry> Rank(IEnumerable<Entry> entries, bool descending = true)
    {
        return entries
            .SetPositions(e => e.Points, descending, (e, pos) => e.Position = pos)
            .ToList();
    }

    [Fact]
    public void SetPositions_OrdersDescendingAndNumbersFromOne()
    {
        var result = Rank(new[]
        {
            new Entry { Name = "b", Points = 500 },
            new Entry { Name = "a", Points = 900 },
            new Entry { Name = "c", Points = 100 }
        });

        result.Select(e => e.Name).Should().ContainInOrder("a", "b", "c");
        result.Select(e => e.Position).Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public void SetPositions_WhenAscending_ReversesTheOrder()
    {
        var result = Rank(new[]
        {
            new Entry { Name = "b", Points = 500 },
            new Entry { Name = "a", Points = 900 },
            new Entry { Name = "c", Points = 100 }
        }, descending: false);

        result.Select(e => e.Name).Should().ContainInOrder("c", "b", "a");
    }

    [Fact]
    public void SetPositions_TiedEntriesShareTheSamePosition()
    {
        var result = Rank(new[]
        {
            new Entry { Name = "a", Points = 900 },
            new Entry { Name = "b", Points = 900 },
            new Entry { Name = "c", Points = 100 }
        });

        result.Single(e => e.Name == "a").Position.Should().Be(1);
        result.Single(e => e.Name == "b").Position.Should().Be(1);
    }

    [Fact]
    public void SetPositions_AfterATieTheNextPositionSkipsAhead()
    {
        // classement sportif : deux premiers ex aequo, le suivant est 3e et non 2e
        var result = Rank(new[]
        {
            new Entry { Name = "a", Points = 900 },
            new Entry { Name = "b", Points = 900 },
            new Entry { Name = "c", Points = 100 }
        });

        result.Single(e => e.Name == "c").Position.Should().Be(3);
    }

    [Fact]
    public void SetPositions_WhenAllTied_EveryoneIsFirst()
    {
        var result = Rank(new[]
        {
            new Entry { Name = "a", Points = 42 },
            new Entry { Name = "b", Points = 42 },
            new Entry { Name = "c", Points = 42 }
        });

        result.Should().OnlyContain(e => e.Position == 1);
    }

    [Fact]
    public void SetPositions_WhenEmpty_ReturnsEmptyList()
    {
        Rank(new List<Entry>()).Should().BeEmpty();
    }

    [Fact]
    public void SetPositions_WhenSingleEntry_IsFirst()
    {
        Rank(new[] { new Entry { Name = "a", Points = 7 } })
            .Single().Position.Should().Be(1);
    }
}
