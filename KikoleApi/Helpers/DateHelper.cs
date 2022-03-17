﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleApi.Helpers
{
    internal static class DateHelper
    {
        public static TimeSpan Average(this IEnumerable<TimeSpan> spans)
        {
            return TimeSpan.FromSeconds(spans.Select(s => s.TotalSeconds).Average());
        }
    }
}