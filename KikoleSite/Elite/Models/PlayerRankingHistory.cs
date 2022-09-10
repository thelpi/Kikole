using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Elite.Models
{
    public class PlayerRankingHistory
    {
        private static readonly int[] RankMilestones = new int[]
        {
            100, 50, 20, 10, 5, 3, 1
        };
        private static readonly int[] PointsMilestones = new int[]
        {
            1, 1000, 2000, 3000, 4000, 5000
        };

        public IReadOnlyCollection<PlayerRankingLight> RankingDetails { get; }
        public PlayerRankingLight BestPointsRanking { get; }
        public PlayerRankingLight BestTimeRanking { get; }
        public IReadOnlyDictionary<int, DateTime> PointsHighlights { get; }
        public IReadOnlyDictionary<int, DateTime> PointsRankHighlights { get; }
        public IReadOnlyDictionary<DateTime, int> PointsRankHistory { get; }

        public PlayerRankingHistory(IReadOnlyCollection<PlayerRankingLight> rankings)
        {
            RankingDetails = rankings;

            BestPointsRanking = rankings.First(_ => _.PointsRank == rankings.Min(r => r.PointsRank));
            BestTimeRanking = rankings.First(_ => _.TimeRank == rankings.Min(r => r.TimeRank));

            var rkm = new Dictionary<int, DateTime>();
            foreach (var k in RankMilestones)
            {
                var okRk = rankings.FirstOrDefault(_ => _.PointsRank <= k);
                if (okRk != null)
                    rkm.Add(k, okRk.Date);
            }

            PointsRankHighlights = rkm;

            var rpm = new Dictionary<int, DateTime>();
            foreach (var pk in PointsMilestones)
            {
                var okRk = rankings.FirstOrDefault(_ => _.Points >= pk);
                if (okRk != null)
                    rpm.Add(pk, okRk.Date);
            }

            PointsHighlights = rpm;

            var groupHistoryRank = new Dictionary<DateTime, int>();
            int currentRank = -1;
            var counter = 1;
            foreach (var rk in rankings)
            {
                if (currentRank != rk.PointsRank)
                {
                    groupHistoryRank.Add(rk.Date, rk.PointsRank);
                    currentRank = rk.PointsRank;
                }
                else if (counter == rankings.Count)
                {
                    groupHistoryRank.Add(rk.Date, rk.PointsRank);
                }
                counter++;
            }

            PointsRankHistory = groupHistoryRank;
        }
    }
}
