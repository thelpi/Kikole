using System;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Models
{
    public class PlayerRankingLight
    {
        public int PointsRank { get; set; }
        public int TimeRank { get; set; }
        public int Points { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime Date { get; set; }
        public long PlayerId { get; set; }
        public Game Game { get; set; }
    }
}
