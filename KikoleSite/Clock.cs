using System;

namespace KikoleSite;

/// <summary>
/// Clock implementation.
/// </summary>
/// <seealso cref="IClock"/>
public class Clock : IClock
{
    /// <inheritdoc />
    public DateTime Now => DateTime.Now;

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(Now);

    /// <inheritdoc />
    public DateOnly Tomorrow => Today.AddDays(1);

    /// <inheritdoc />
    public DateOnly Yesterday => Today.AddDays(-1);

    /// <inheritdoc />
    public DateOnly FirstOfMonth => new(Now.Year, Now.Month, 1);

    /// <inheritdoc />
    public DateTime TomorrowEnd => Tomorrow.ToDateTime(new TimeOnly(23, 59, 59));

    /// <inheritdoc />
    public DateTime NowSeconds => Now.AddMilliseconds(-Now.Millisecond);

    /// <inheritdoc />
    public bool IsTomorrowIn(int minutes)
    {
        return Now.AddMinutes(minutes) >= Tomorrow.ToDateTime(TimeOnly.MinValue);
    }
}
