using System;
using KikoleSite.Enums;

namespace KikoleSite.Dtos
{
    public class RankingDto
    {
        public ulong Id { get; set; }
        public uint PlayerId { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public DateTime Date { get; set; }
        public int Points { get; set; }
        public int Rank { get; set; }
        public int Time { get; set; }
        public uint EntryId { get; set; }
    }
}
