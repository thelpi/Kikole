using System;
using System.Collections.Generic;
using KikoleSite.Enums;

namespace KikoleSite.Models
{
    public class LatestPoint
    {
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public int Time { get; set; }
        public int Points { get; set; }
        public IReadOnlyDictionary<Player, DateTime> Occurences { get; set; }
    }
}
