using System;
using System.Collections.Generic;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.ViewDatas
{
    public class StandingItemData
    {
        public int Rank { get; set; }
        public int Days { get; set; }
        public IReadOnlyList<TimeSpan> Times { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public string PlayerName { get; set; }
        public string PlayerColor { get; set; }
        public DateTime Date { get; set; }
        public string NextPlayerName { get; set; }
        public string NextPlayerColor { get; set; }
        public DateTime? NextDate { get; set; }

        public TimeSpan Time => Times[0];
    }
}
