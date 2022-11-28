using System;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.ViewDatas
{
    public class LatestPointsItemData
    {
        public int Rank { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public TimeSpan Time { get; set; }
        public int Points { get; set; }
        public int Occurences { get; set; }
        public DateTime LatestDate { get; set; }
        public string LatestPlayerName { get; set; }
        public string LatestPlayerColor { get; set; }
        public int Days { get; set; }
    }
}
