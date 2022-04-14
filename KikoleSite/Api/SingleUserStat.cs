using System;

namespace KikoleSite.Api
{
    public class SingleUserStat
    {
        public DateTime Date { get; set; }

        public string Answer { get; set; }

        public bool Attempt { get; set; }

        public TimeSpan? Time => TimeSec.HasValue ? new TimeSpan(0, 0, TimeSec.Value) : default(TimeSpan?);

        public int? TimeSec { get; set; }

        public int? Points { get; set; }

        public int? TimePosition { get; set; }

        public int? PointsPosition { get; set; }
    }
}
