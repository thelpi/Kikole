﻿using System;

namespace KikoleSite.Dtos
{
    public class RankingEntryDto
    {
        public uint RankingId { get; set; }
        public uint PlayerId { get; set; }
        public int Points { get; set; }
        public int Rank { get; set; }
        public int Time { get; set; }
        public uint EntryId { get; set; }
        public DateTime? EntryDate { get; set; }

        internal void SetPoints()
        {
            Points = Rank == 1 ? 100 : (Rank == 2 ? 97 : Math.Max((100 - Rank) - 2, 0));
        }
    }
}
