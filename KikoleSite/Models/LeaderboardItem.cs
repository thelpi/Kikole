using System;
using KikoleSite.Helpers;

namespace KikoleSite.Models;

public class LeaderboardItem
{
    public int Rank { get; set; }
    public ulong UserId { get; set; }
    public required string UserName { get; set; }
    public int Points { get; set; }
    public TimeSpan BestTime { get; set; }
    public int KikolesFound { get; set; }
    public int KikolesAttempted { get; set; }
    public int KikolesProposed { get; set; }

    // ugly, but easier here than in JS
    public string BestTimeString => BestTime.ToNaString();

    // alimentes uniquement par LeaderSorts.BadgePercentage (cf. LeaderService
    // .GetBadgePercentageLeaderboardAsync) : laisses a leur defaut pour tout autre tri,
    // jamais lus par la vue dans ce cas-la.
    public int BadgesFound { get; set; }
    public int BadgesMissing { get; set; }
    public double BadgePercentage { get; set; }

    /// <summary>Moyenne, sur les badges obtenus par cet utilisateur, de la proportion des
    /// <em>autres</em> joueurs du classement general qui ne les ont pas (100 = seul detenteur,
    /// 0 = tout le monde l'a). <c>null</c> si aucun badge (0/0).</summary>
    public double? AverageBadgeRarity { get; set; }

    public string BadgePercentageString => $"{Math.Round(BadgePercentage)}%";
    public string AverageBadgeRarityString => AverageBadgeRarity.HasValue ? $"{Math.Round(AverageBadgeRarity.Value)}%" : "-";
}
