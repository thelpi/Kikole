using System;
using System.Text.RegularExpressions;

namespace KikoleSite.Helpers;

/// <summary>
/// Garde-fou des requetes de badge stockees en base (<c>badges.condition_sql</c>) : une seule
/// instruction SELECT, sans ecriture de fichier. S'ajoute a l'execution en transaction en lecture seule.
/// </summary>
internal static partial class BadgeRuleSql
{
    internal static bool IsAllowed(string? sql)
    {
        return !string.IsNullOrWhiteSpace(sql)
            && sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && !sql.Contains(';')
            && !IntoKeyword().IsMatch(sql);
    }

    [GeneratedRegex(@"\bINTO\b", RegexOptions.IgnoreCase)]
    private static partial Regex IntoKeyword();
}