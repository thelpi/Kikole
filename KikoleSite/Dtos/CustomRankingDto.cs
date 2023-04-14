using System;
using KikoleSite.Enums;

namespace KikoleSite.Dtos
{
    public class CustomRankingDto
    {
        public uint PlayerId { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public int Rank { get; set; }
        public int Time { get; set; }
        public DateTime EntryDate { get; set; }
    }
}
