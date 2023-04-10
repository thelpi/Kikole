using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Enums;

namespace KikoleSite.Models
{
    public class StageLeaderboardItem : IEquatable<StageLeaderboardItem>
    {
        public static readonly IReadOnlyDictionary<LeaderboardGroupOptions, Func<IEnumerable<StageLeaderboardItem>, IEnumerable<StageLeaderboardItem>>> ComputeGroupOtions =
            new Dictionary<LeaderboardGroupOptions, Func<IEnumerable<StageLeaderboardItem>, IEnumerable<StageLeaderboardItem>>>
            {
                { LeaderboardGroupOptions.FirstRankedFirst, _ => _.Take(1) },
                { LeaderboardGroupOptions.Ranked, _ => _.Where(x => x.Points > 0) },
                { LeaderboardGroupOptions.RankedFirst, _ => _.Where(x => x.Rank == 1) },
                { LeaderboardGroupOptions.RankedTop10, _ => _.Where(x => x.Rank <= 10) },
            };

        public static readonly IReadOnlyDictionary<int, int> PointsTiers =
            new Dictionary<int, int>
            {
                { 300, 10 },
                { 290, 9 },
                { 275, 8 },
                { 250, 7 },
                { 225, 6 },
                { 200, 5 },
                { 150, 4 },
                { 100, 3 },
                { 50, 2 },
                { 1, 1 },
                { 0, 0 }
            };

        public Player Player { get; set; }
        public int Points { get; set; }
        public DateTime LatestTime { get; set; }
        public int Rank { get; set; }
        public int Tier => PointsTiers[PointsTiers.Keys.Where(_ => _ <= Points).First()];

        public bool Equals(StageLeaderboardItem other)
        {
            return Player.Id == other.Player.Id
                && Points == other.Points
                && LatestTime == other.LatestTime
                && Rank == other.Rank;
        }
    }

    public class StageLeaderboardItemSamePlayer : IEqualityComparer<StageLeaderboardItem>
    {
        public bool Equals(StageLeaderboardItem x, StageLeaderboardItem y)
        {
            return x.Player.Id == y.Player.Id;
        }

        public int GetHashCode(StageLeaderboardItem obj)
        {
            return obj.GetHashCode();
        }
    }

    public class StageLeaderboardItemSameTier : IEqualityComparer<StageLeaderboardItem>
    {
        public bool Equals(StageLeaderboardItem x, StageLeaderboardItem y)
        {
            return x.Player.Id == y.Player.Id
                && x.Tier == y.Tier;
        }

        public int GetHashCode(StageLeaderboardItem obj)
        {
            return obj.GetHashCode();
        }
    }

    public class StageLeaderboardItemSameRank : IEqualityComparer<StageLeaderboardItem>
    {
        public bool Equals(StageLeaderboardItem x, StageLeaderboardItem y)
        {
            return x.Player.Id == y.Player.Id
                && x.Rank == y.Rank;
        }

        public int GetHashCode(StageLeaderboardItem obj)
        {
            return obj.GetHashCode();
        }
    }
}
