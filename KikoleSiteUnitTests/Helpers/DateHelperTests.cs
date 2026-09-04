using System;
using System.Collections.Generic;
using FluentAssertions;
using KikoleSite.Helpers;
using Xunit;

namespace KikoleSiteUnitTests.Helpers;

public class DateHelperTests
{
    // ------------------------------------------------------------- Average

    [Fact]
    public void Average_WhenEmpty_ReturnsNull()
    {
        new List<TimeSpan>().Average().Should().BeNull();
    }

    [Fact]
    public void Average_ReturnsMeanDuration()
    {
        var spans = new List<TimeSpan>
        {
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(60)
        };

        spans.Average().Should().Be(TimeSpan.FromMinutes(30));
    }

    // ------------------------------------------------------------- ToSeconds / ToRoundMinutes

    [Fact]
    public void ToSeconds_TruncatesTowardsZero()
    {
        TimeSpan.FromMilliseconds(1999).ToSeconds().Should().Be(1);
    }

    [Fact]
    public void ToRoundMinutes_RoundsUp()
    {
        // les deux methodes n'arrondissent pas dans le meme sens : plancher pour les
        // secondes, plafond pour les minutes
        TimeSpan.FromSeconds(61).ToRoundMinutes().Should().Be(2);
        TimeSpan.FromSeconds(60).ToRoundMinutes().Should().Be(1);
    }

    // ------------------------------------------------------------- Min / Max

    [Fact]
    public void Min_ReturnsEarliestDate()
    {
        var early = new DateTime(2026, 1, 1);
        var late = new DateTime(2026, 12, 31);

        early.Min(late).Should().Be(early);
        late.Min(early).Should().Be(early);
    }

    [Fact]
    public void Max_ReturnsLatestDate()
    {
        var early = new DateTime(2026, 1, 1);
        var late = new DateTime(2026, 12, 31);

        early.Max(late).Should().Be(late);
        late.Max(early).Should().Be(late);
    }

    [Fact]
    public void MinAndMax_WhenEqual_ReturnThatDate()
    {
        var d = new DateTime(2026, 6, 15);

        d.Min(d).Should().Be(d);
        d.Max(d).Should().Be(d);
    }

    // ------------------------------------------------------------- IsFirstOfMonth

    [Theory]
    [InlineData("2026-09-01", "2026-09-15", true)]
    [InlineData("2026-09-02", "2026-09-15", false)]  // pas le 1er
    [InlineData("2026-08-01", "2026-09-15", false)]  // autre mois
    [InlineData("2025-09-01", "2026-09-15", false)]  // autre annee
    public void IsFirstOfMonth_ChecksSameMonthAndFirstDay(string date, string reference, bool expected)
    {
        DateTime.Parse(date).IsFirstOfMonth(DateTime.Parse(reference)).Should().Be(expected);
    }

    [Fact]
    public void IsFirstOfMonth_IgnoresTimeOfDay()
    {
        new DateTime(2026, 9, 1, 23, 59, 59)
            .IsFirstOfMonth(new DateTime(2026, 9, 15, 8, 0, 0))
            .Should().BeTrue();
    }

    // ------------------------------------------------------------- IsAfterInMonth

    [Theory]
    [InlineData("2026-09-15", "2026-09-15", true)]   // borne incluse
    [InlineData("2026-09-16", "2026-09-15", true)]
    [InlineData("2026-09-14", "2026-09-15", false)]
    [InlineData("2026-10-16", "2026-09-15", false)]  // mois different
    public void IsAfterInMonth_ChecksSameMonthAndDayAtOrAfter(string date, string reference, bool expected)
    {
        DateTime.Parse(date).IsAfterInMonth(DateTime.Parse(reference)).Should().Be(expected);
    }

    // ------------------------------------------------------------- IsEndOfMonth

    [Theory]
    [InlineData("2026-09-30", "2026-09-15", true)]   // 30 jours
    [InlineData("2026-09-29", "2026-09-15", false)]
    [InlineData("2026-01-31", "2026-01-15", true)]   // 31 jours
    [InlineData("2026-02-28", "2026-02-15", true)]   // fevrier commun
    [InlineData("2024-02-29", "2024-02-15", true)]   // fevrier bissextile
    [InlineData("2024-02-28", "2024-02-15", false)]  // veille du 29 en bissextile
    public void IsEndOfMonth_HandlesVariableMonthLengths(string date, string reference, bool expected)
    {
        DateTime.Parse(date).IsEndOfMonth(DateTime.Parse(reference)).Should().Be(expected);
    }
}
