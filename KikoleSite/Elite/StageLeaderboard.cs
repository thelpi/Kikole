using System;
using System.Collections.Generic;

namespace KikoleSite.Elite
{
    public class StageLeaderboard
    {
        internal const int BasePoints = 100;
        internal static readonly IReadOnlyDictionary<int, int> PointsChart = new Dictionary<int, int>
        {
            { 100, 97 },
            { 97, 95 },
            { 0, 0 },
        };

        public IReadOnlyCollection<StageLeaderboardItem> Items { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public Stage Stage { get; set; }
    }
}
