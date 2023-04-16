using System;

namespace KikoleSite.Dtos
{
    public class BaseRankingEntryDto
    {
        public uint PlayerId { get; set; }
        public int Points { get; set; }
        public int Rank { get; set; }
        public int Time { get; set; }
        public uint EntryId { get; set; }
        public DateTime? EntryDate { get; set; }
    }
}
