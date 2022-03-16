using System;

namespace KikoleApi.Models
{
    public class SingleUserStat
    {
        public DateTime Date { get; set; }

        public string Answer { get; set; }

        public bool Tried { get; set; }

        public bool SuccessFul { get; set; }

        public TimeSpan Time { get; set; }

        public int Points { get; set; }

        public int TimePosition { get; set; }

        public int PointsPosition { get; set; }
    }
}
