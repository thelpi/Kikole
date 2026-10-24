using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KikoleSite.Helpers;

internal static class StringHelper
{
    private const string Iso8859Code = "ISO-8859-8";
    private const char Separator = ';';

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

            // sans equivalent ni en decomposition NFD ni dans la table de rabattement :
            // la ligature neerlandaise, le L point-median catalan, et quelques archaismes.
            // Sans eux le caractere devient '?', que personne ne peut saisir au clavier.
            { 'Ĳ', "ij" }, { 'ĳ', "ij" },
            { 'Ŀ', "l" },  { 'ŀ', "l" },
            { 'Ŋ', "n" },  { 'ŋ', "n" }, { 'ŉ', "n" },
            { 'ĸ', "k" },
            { 'ſ', "s" },
        };

    // une faute de frappe est tolérée dès 6 lettres, puis une de plus par tranche de 5 ; la
    // tolérance suit la longueur du plus long des deux noms, jamais celle de la seule proposition
    // (sinon "rodri" passerait pour "modric")
    internal static bool ContainsApproximately(this string source, string value)
    {
        var cleanValue = value.Sanitize();

        // rien d'exploitable apres normalisation (ex. un accent combinant seul)
        if (cleanValue.Length == 0)
            return false;

        return source.Disjoin().Any(allowed =>
            allowed.GetEditDistance(cleanValue) <= MaxTypos(Math.Max(allowed.Length, cleanValue.Length)));
    }

    private static int MaxTypos(int length)
    {
        return (length - 1) / 5;
    }

    private static bool IsWithinTypos(string a, string b)
    {
        return a.GetEditDistance(b) <= MaxTypos(Math.Max(a.Length, b.Length));
    }

    private static List<string> SplitWords(string sanitizedValue)
    {
        return new string(sanitizedValue.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static bool IsSameWord(string a, string b)
    {
        return IsWithinTypos(a, b)
            || (a.Length == 1 && b.StartsWith(a, StringComparison.Ordinal))
            || (b.Length == 1 && a.StartsWith(b, StringComparison.Ordinal));
    }

    // meme tolerance aux fautes que ContainsApproximately, en plus souple sur la forme : mots dans
    // le desordre (prenom/nom inverses), nom seul ou prenom seul, initiale, ponctuation et espaces
    internal static bool IsSimilarName(this string value, string other)
    {
        var a = SplitWords(value.Sanitize());
        var b = SplitWords(other.Sanitize());
        if (a.Count == 0 || b.Count == 0)
            return false;

        if (IsWithinTypos(string.Join(' ', a), string.Join(' ', b))
            || IsWithinTypos(string.Concat(a), string.Concat(b)))
            return true;

        var (shorter, longer) = a.Count <= b.Count ? (a, b) : (b, a);
        var remaining = new List<string>(longer);
        foreach (var word in shorter)
        {
            var index = remaining.FindIndex(w => IsSameWord(word, w));
            if (index < 0)
                return false;

            remaining.RemoveAt(index);
        }

        return true;
    }

    // la mauvaise proposition deja faite qui ressemble le plus a la nouvelle, sans compter la meme
    // proposition a la casse et aux accents pres (deja non facturee une seconde fois)
    internal static string? FindSimilarName(this IEnumerable<string> previousNames, string value)
    {
        var sanitizedValue = value.Sanitize();

        return previousNames
            .Where(p => p.Sanitize() != sanitizedValue && value.IsSimilarName(p))
            .OrderBy(p => p.Sanitize().GetEditDistance(sanitizedValue))
            .FirstOrDefault();
    }
    internal static IReadOnlyList<string> Disjoin(this string value)
    {
        return value.Split(Separator).ToList();
    }

    internal static string Sanitize(this string value)
    {
        return value.Trim().RemoveDiacritics().ToLowerInvariant();
    }

    // recherche tolerante (autocompletion) : ne garde que les lettres et chiffres, pour que
    // "saint etienne", "saint-etienne" et "milan ac" trouvent "Saint-Étienne" / "Milan A.C."
    internal static string SanitizeForSearch(this string value)
    {
        return new string(value.Sanitize().Where(char.IsLetterOrDigit).ToArray());
    }

    internal static string SanitizeJoin(this IEnumerable<string> values, string sourceValue)
    {
        return string.Join(Separator, values.Select(Sanitize).Concat(new[] { sourceValue.Sanitize() }).Distinct());
    }

    // faux pour une saisie non latine abimee par l'ancienne normalisation : il n'y reste que des '?' et des espaces
    internal static bool HasReadableText(this string value)
    {
        return value.Any(c => c != '?' && !char.IsWhiteSpace(c));
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

        // la decomposition de compatibilite replie aussi les formes pleine largeur
        // (ex. "ＹＡＭＡＤＡ", frequentes avec un clavier japonais) et les ligatures
        var decomposed = mapped.ToString().Normalize(NormalizationForm.FormKD);

        var stripped = new StringBuilder(decomposed.Length);
        // seuls les accents des lettres latines sont retires : le signe d'un kana voise
        // (が, ば...) ou une voyelle hebraique font partie de la lettre
        foreach (var c in decomposed)
        {
            var isMark = CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark;
            if (isMark && (stripped.Length == 0 || IsLatinLetter(stripped[^1])))
                continue;

            stripped.Append(c);
        }

        // le best-fit de la page de code rabat les lettres latines restantes sur leur
        // equivalent ASCII (o, l, ae...), y compris celles sans decomposition Unicode ;
        // tout caractere qu'il ne sait pas rabattre (japonais, cyrillique, coreen...) est
        // conserve tel quel plutot que remplace par '?'
        var result = new StringBuilder(stripped.Length);
        foreach (var c in stripped.ToString())
        {
            if (c < 128)
            {
                result.Append(c);
                continue;
            }

            var bytes = BestFitEncoding.GetBytes(c.ToString());
            result.Append(bytes.Length == 1 && bytes[0] < 128 && bytes[0] != (byte)'?' ? (char)bytes[0] : c);
        }

        // recompose notamment les syllabes coreennes eclatees par la normalisation precedente
        return result.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsLatinLetter(char c)
    {
        return c is >= 'A' and <= 'Z' or >= 'a' and <= 'z'
            or >= '\u00C0' and < '\u0250'
            or >= '\u1E00' and <= '\u1EFF';
    }

    private static Encoding ResolveBestFitEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(Iso8859Code);
    }

    // distance d'edition (insertion, suppression, substitution) ou l'inversion de deux
    // lettres adjacentes compte pour une seule faute
    internal static int GetEditDistance(this string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        if (n == 0)
            return m;
        if (m == 0)
            return n;

        var d = new int[n + 1, m + 1];
        for (var i = 0; i <= n; i++)
            d[i, 0] = i;
        for (var j = 0; j <= m; j++)
            d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);

                if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        }

        return d[n, m];
    }
    internal static bool IsEnumValue<T>(this string? value)
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
