using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KikoleSite.Helpers
{
    internal static class StringHelper
    {
        private const string Iso8859Code = "ISO-8859-8";
        private const char Separator = ';';
        private const decimal NameToleranceMax = 0.5M;

        // la page de code n'est pas disponible tant que CodePagesEncodingProvider n'est pas
        // enregistre ; on le fait ici pour que le helper reste utilisable hors du host web
        private static readonly Encoding BestFitEncoding = ResolveBestFitEncoding();

        // lettres latines sans decomposition Unicode et absentes de la table best-fit
        // de la page de code : sans cette table elles deviendraient des '?'
        private static readonly IReadOnlyDictionary<char, string> UnmappedLetters =
            new Dictionary<char, string>
            {
                { 'ß', "ss" }, { 'ẞ', "ss" },
                { 'Þ', "th" }, { 'þ', "th" },
                { 'Ð', "d" },  { 'ð', "d" },
            };

        internal static bool ContainsApproximately(this string source, string value)
        {
            return source.Disjoin().Any(_ =>
            {
                var cleanValue = value.Sanitize();
                var score = _.GetLevenshteinDistance(cleanValue);
                return (score / (decimal)cleanValue.Length) < NameToleranceMax;
            });
        }

        internal static bool ContainsSanitized(this string source, string value)
        {
            return source.Disjoin().Contains(value.Sanitize());
        }

        internal static IReadOnlyList<string> Disjoin(this string value)
        {
            return value.Split(Separator).ToList();
        }

        internal static string Sanitize(this string value)
        {
            return value.Trim().RemoveDiacritics().ToLowerInvariant();
        }

        internal static string SanitizeJoin(this IEnumerable<string> values, string sourceValue)
        {
            return string.Join(Separator, values.Select(Sanitize).Concat(new[] { sourceValue.Sanitize() }).Distinct());
        }

        internal static bool IsValid(this IReadOnlyList<string>? values)
        {
            return values?.Count > 0
                && values.All(v => !string.IsNullOrWhiteSpace(v));
        }

        internal static string RemoveDiacritics(this string value)
        {
            var mapped = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (UnmappedLetters.TryGetValue(c, out var replacement))
                    mapped.Append(replacement);
                else
                    mapped.Append(c);
            }

            var decomposed = mapped.ToString().Normalize(NormalizationForm.FormD);

            var stripped = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stripped.Append(c);
            }

            // le best-fit de la page de code rabat les lettres latines restantes sur leur
            // equivalent ASCII (o, l, ae...), y compris celles sans decomposition Unicode
            var tempBytes = BestFitEncoding.GetBytes(stripped.ToString());
            return Encoding.UTF8.GetString(tempBytes);
        }

        private static Encoding ResolveBestFitEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(Iso8859Code);
        }

        internal static int GetLevenshteinDistance(this string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (var i = 0; i <= n; d[i, 0] = i++) ;
            for (var j = 1; j <= m; d[0, j] = j++) ;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    var min1 = d[i - 1, j] + 1;
                    var min2 = d[i, j - 1] + 1;
                    var min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
        }

        internal static bool IsEnumValue<T>(this string value)
        {
            if (!typeof(T).IsEnum)
                throw new InvalidOperationException("The targeted type should be an enum.");

            if (value == null)
                return false;

            if (int.TryParse(value, out var enumId))
            {
                if (!Enum.GetValues(typeof(T)).Cast<int>().Contains(enumId))
                    return false;
            }
            else
            {
                if (!Enum.IsDefined(typeof(T), value))
                    return false;
            }

            return true;
        }
    }
}
