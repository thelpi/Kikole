using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KikoleApi.Helpers
{
    internal static class StringHelper
    {
        const string Iso8859Code = "ISO-8859-8";
        const char Separator = ';';

        internal static string Sanitize(this string value)
        {
            return value.Trim().RemoveDiacritics().ToLowerInvariant();
        }

        internal static string SanitizeJoin(this IEnumerable<string> values, string sourceValue)
        {
            return string.Join(Separator, values.Select(Sanitize).Concat(new[] { sourceValue.Sanitize() }).Distinct());
        }

        internal static bool IsValid(this IReadOnlyList<string> values)
        {
            return values?.Count > 0
                && values.All(v => !string.IsNullOrWhiteSpace(v));
        }

        internal static string RemoveDiacritics(this string value)
        {
            var tempBytes = Encoding.GetEncoding(Iso8859Code).GetBytes(value);
            return Encoding.UTF8.GetString(tempBytes);
        }
    }
}
