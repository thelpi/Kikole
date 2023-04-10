using System;
using KikoleSite.Enums;

namespace KikoleSite.Models
{
    public class PlayerStageLevelRankingLight
    {
        public int Points { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime Date { get; set; }
        public long PlayerId { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
    }
}
