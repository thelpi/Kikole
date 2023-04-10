using System;

namespace KikoleSite.ViewDatas
{
    public class TimeRankingItemData
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public string PlayerColor { get; set; }
        public TimeSpan EasyTime { get; set; }
        public TimeSpan MediumTime { get; set; }
        public TimeSpan HardTime { get; set; }
        public TimeSpan TotalTime { get; set; }
    }
}
