using System;
using System.Collections.Generic;
using System.Dynamic;

namespace KikoleSite
{
    internal static class Helper
    {
        const string NA = "N/A";
        const string TimeSpanPattern = @"hh\:mm";

        internal static bool IsPropertyExist(dynamic settings, string name)
        {
            return settings is ExpandoObject
                ? ((IDictionary<string, object>)settings).ContainsKey(name)
                : settings.GetType().GetProperty(name) != null;
        }

        internal static string ToNaString(this object data)
        {
            return data?.ToString() ?? NA;
        }

        internal static string ToNaString(this TimeSpan? data)
        {
            return data?.ToString(TimeSpanPattern) ?? NA;
        }

        internal static string ToYesNo(this bool data)
        {
            return data ? "Yes" : "No";
        }
    }
}
