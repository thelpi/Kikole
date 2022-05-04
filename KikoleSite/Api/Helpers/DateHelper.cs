using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Api.Helpers
{
    internal static class DateHelper
    {
        internal static TimeSpan? Average(this IEnumerable<TimeSpan> spans)
        {
            return !spans.Any()
                ? default(TimeSpan?)
                : TimeSpan.FromSeconds(spans.Select(s => s.TotalSeconds).Average());
        }

        internal static int? ToSeconds(this TimeSpan? ts)
        {
            return ts.HasValue ? ts.Value.ToSeconds() : default(int?);
        }

        internal static int ToSeconds(this TimeSpan ts)
        {
            return (int)Math.Floor(ts.TotalSeconds);
        }

        internal static int ToRoundMinutes(this TimeSpan ts)
        {
            return (int)Math.Ceiling(ts.TotalMinutes);
        }
    }
}
