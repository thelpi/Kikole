using System.Collections.Generic;
using System.Linq;

namespace KikoleApi
{
    internal static class StringHelper
    {
        internal static string Sanitize(this string value)
        {
            return value.Trim().ToLowerInvariant();
        }

        internal static string SanitizeJoin(this IEnumerable<string> values)
        {
            return string.Join(';', values.Select(Sanitize));
        }

        internal static bool IsValid(this IReadOnlyList<string> values)
        {
            return values?.Count > 0
                && values.All(v => !string.IsNullOrWhiteSpace(v));
        }
    }
}
