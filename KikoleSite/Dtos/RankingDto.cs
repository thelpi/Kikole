using System;
using KikoleSite.Enums;

namespace KikoleSite.Dtos
{
    public class RankingDto
    {
        public uint Id { get; set; }
        public DateTime Date { get; set; }
        public NoDateEntryRankingRule Rule { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
    }
}
