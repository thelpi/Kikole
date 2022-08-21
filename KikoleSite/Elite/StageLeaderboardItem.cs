using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Elite
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

        public Player Player { get; set; }
        public int Points { get; set; }
        public DateTime LatestTime { get; set; }
        public int Rank { get; set; }

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
}
