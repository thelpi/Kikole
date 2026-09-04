using System;
using FluentAssertions;
using KikoleSite;
using Xunit;

namespace KikoleSiteUnitTests;

/// <summary>
/// Le calendrier lui-meme ne depend de rien : il porte trois dates deduites d'une seule
/// origine, et refuse d'etre lu avant son amorcage.
/// </summary>
public class GameCalendarTests
{
    private static readonly DateTime Hidden = new(2022, 3, 2);

    private static GameCalendar Started(DateTime hiddenDate)
    {
        var calendar = new GameCalendar();
        calendar.Initialize(hiddenDate);
        return calendar;
    }

    [Fact]
    public void FirstDate_IsTheDayAfterTheHiddenDay()
    {
        Started(Hidden).FirstDate.Should().Be(Hidden.AddDays(1));
    }

    [Fact]
    public void FirstMonth_IsTheFirstDayOfTheMonthOfFirstDate()
    {
        // la journee cachee du 31 janvier place la premiere journee en fevrier
        Started(new DateTime(2022, 1, 31)).FirstMonth.Should().Be(new DateTime(2022, 2, 1));
    }

    [Fact]
    public void TheTimeOfDayIsDropped()
    {
        Started(Hidden.AddHours(17).AddMinutes(42)).HiddenDate.Should().Be(Hidden);
    }

    [Fact]
    public void ReadingADateBeforeStartup_Fails()
    {
        Action act = () => _ = new GameCalendar().FirstDate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*amorce*");
    }

    [Fact]
    public void InitializingTwice_Fails()
    {
        var calendar = Started(Hidden);

        Action act = () => calendar.Initialize(Hidden);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*deja*");
    }
}
