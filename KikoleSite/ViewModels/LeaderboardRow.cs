using System;
using KikoleSite.Helpers;
using KikoleSite.Models;

namespace KikoleSite.ViewModels;

/// <summary>
/// Ligne du classement general telle qu'affichee : tout est deja formate, que la ligne soit
/// rendue par la vue ou renvoyee en JSON a <c>site.js</c>. Le formatage (temps, pourcentages,
/// tiret) vit ici et pas dans <see cref="LeaderboardItem"/>, qui reste un modele sans
/// notion de presentation ; il est evalue a la construction, donc avec la culture de la
/// requete qui l'a produit.
/// </summary>
public class LeaderboardRow
{
    public int Rank { get; init; }
    public ulong UserId { get; init; }
    public required string UserName { get; init; }
    public int Points { get; init; }
    public int KikolesFound { get; init; }
    public int KikolesAttempted { get; init; }
    public int KikolesProposed { get; init; }
    public int BadgesFound { get; init; }
    public int BadgesMissing { get; init; }
    public required string BestTimeString { get; init; }
    public required string BadgePercentageString { get; init; }
    public required string AverageBadgeRarityString { get; init; }

    public static LeaderboardRow From(LeaderboardItem item) => new()
    {
        Rank = item.Rank,
        UserId = item.UserId,
        UserName = item.UserName,
        Points = item.Points,
        KikolesFound = item.KikolesFound,
        KikolesAttempted = item.KikolesAttempted,
        KikolesProposed = item.KikolesProposed,
        BadgesFound = item.BadgesFound,
        BadgesMissing = item.BadgesMissing,
        BestTimeString = item.BestTime.ToNaString(),
        BadgePercentageString = $"{Math.Round(item.BadgePercentage)}%",
        AverageBadgeRarityString = item.AverageBadgeRarity.HasValue
            ? $"{Math.Round(item.AverageBadgeRarity.Value)}%"
            : "-"
    };
}
