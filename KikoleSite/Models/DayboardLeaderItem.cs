using System;

namespace KikoleSite.Models
{
    public class DayboardLeaderItem : DayboardItem
    {
        public int Rank { get; set; }
        public int Points { get; set; }
        public TimeSpan Time { get; set; }
        public bool IsCreator { get; set; }
    }
}
