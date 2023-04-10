using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Models
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

        private readonly DateTime _requestedDate;

        public IReadOnlyCollection<PlayerRankingLight> RankingDetails { get; }
        public PlayerRankingLight BestPointsRanking { get; }
        public PlayerRankingLight BestTimeRanking { get; }
        public IReadOnlyCollection<DateInfo> PointsHighlights { get; }
        public IReadOnlyCollection<DateInfo> PointsRankHighlights { get; }
        public IReadOnlyCollection<DateInfo> PointsRankHistory { get; }
        public DateTime RequestedDate => _requestedDate.Date;

        public PlayerRankingHistory(IReadOnlyCollection<PlayerRankingLight> rankings, DateTime date)
        {
            RankingDetails = rankings;

            BestPointsRanking = rankings.First(_ => _.PointsRank == rankings.Min(r => r.PointsRank));
            BestTimeRanking = rankings.First(_ => _.TimeRank == rankings.Min(r => r.TimeRank));

            var rkm = new List<DateInfo>();
            foreach (var k in RankMilestones)
            {
                var okRk = rankings.FirstOrDefault(_ => _.PointsRank <= k);
                if (okRk != null)
                    rkm.Add(new DateInfo(k, okRk.Date));
            }

            PointsRankHighlights = rkm;

            var rpm = new List<DateInfo>();
            foreach (var pk in PointsMilestones)
            {
                var okRk = rankings.FirstOrDefault(_ => _.Points >= pk);
                if (okRk != null)
                    rpm.Add(new DateInfo(pk, okRk.Date));
            }

            PointsHighlights = rpm;

            var groupHistoryRank = new List<DateInfo>();
            var currentRank = -1;
            var counter = 1;
            foreach (var rk in rankings)
            {
                if (currentRank != rk.PointsRank)
                {
                    groupHistoryRank.Add(new DateInfo(rk.PointsRank, rk.Date));
                    currentRank = rk.PointsRank;
                }
                else if (counter == rankings.Count)
                {
                    groupHistoryRank.Add(new DateInfo(rk.PointsRank, rk.Date));
                }
                counter++;
            }

            PointsRankHistory = groupHistoryRank;
            _requestedDate = date;
        }

        public struct DateInfo
        {
            public int Value { get; }
            public DateTime Date { get; }

            public DateInfo(int value, DateTime date)
            {
                Value = value;
                Date = date;
            }
        }
    }
}
