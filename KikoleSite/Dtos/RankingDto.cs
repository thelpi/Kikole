using System;
using KikoleSite.Enums;

namespace KikoleSite.Dtos
{
    public class RankingDto
    {
        public long RankingTypeId { get; set; }
        public long PlayerId { get; set; }
        public long Time { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public DateTime Date { get; set; }
        public int Rank { get; set; }
        public DateTime EntryDate { get; set; }
        public bool IsSimulatedDate { get; set; }
    }
}
