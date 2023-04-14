using System;

namespace KikoleSite.Dtos
{
    public class CustomRankingDto : RankingDto
    {
        public DateTime EntryDate { get; set; }
        public bool IsSimulatedDate { get; set; }
    }
}
