using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Helpers
{
    internal static class DateHelper
    {
        internal static TimeSpan? Average(this IEnumerable<TimeSpan> spans)
        {
            return !spans.Any()
                ? default(TimeSpan?)
                : TimeSpan.FromSeconds(spans.Select(s => s.TotalSeconds).Average());
        }

        internal static int ToSeconds(this TimeSpan ts)
        {
            return (int)Math.Floor(ts.TotalSeconds);
        }

        internal static int ToRoundMinutes(this TimeSpan ts)
        {
            return (int)Math.Ceiling(ts.TotalMinutes);
        }

        internal static DateTime Min(this DateTime dt1, DateTime dt2)
        {
            return dt1 > dt2 ? dt2 : dt1;
        }

        internal static DateTime Max(this DateTime dt1, DateTime dt2)
        {
            return dt1 < dt2 ? dt2 : dt1;
        }

        internal static bool IsFirstOfMonth(this DateTime date, DateTime reference)
        {
            reference = reference.Date;
            date = date.Date;
            return date.Year == reference.Year
                && date.Month == reference.Month
                && date.Day == 1;
        }

        internal static bool IsAfterInMonth(this DateTime date, DateTime reference)
        {
            reference = reference.Date;
            date = date.Date;
            return date.Year == reference.Year
                && date.Month == reference.Month
                && date.Day >= reference.Day;
        }

        internal static bool IsEndOfMonth(this DateTime date, DateTime reference)
        {
            reference = reference.Date;
            date = date.Date;
            return date.Year == reference.Year
                && date.Month == reference.Month
                && date.AddDays(1).Month > reference.Month;
        }
    }
}
