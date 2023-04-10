using System;
using KikoleSite.Enums;

namespace KikoleSite.Models
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

        public string TimeString => Time.ToString("c");

        public bool HasChanged(PlayerRankingLight other)
        {
            // assuming same player obviously
            return Points != other.Points
                || PointsRank != other.PointsRank
                || Time != other.Time
                || TimeRank != other.TimeRank;
        }
    }
}
