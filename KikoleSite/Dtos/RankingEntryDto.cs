﻿namespace KikoleSite.Dtos
{
    public class RankingEntryDto
    {
        public uint RankingId { get; set; }
        public uint PlayerId { get; set; }
        public int Points { get; set; }
        public int Rank { get; set; }
        public int Time { get; set; }
        public uint EntryId { get; set; }
    }
}
