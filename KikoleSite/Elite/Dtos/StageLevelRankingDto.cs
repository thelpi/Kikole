using System;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Dtos
{
    public class StageLevelRankingDto
    {
        public long Id { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public long PlayerId { get; set; }
        public DateTime Date { get; set; }
        public int Points { get; set; }
        public int Time { get; set; }
        public int Rank { get; set; }
    }
}
