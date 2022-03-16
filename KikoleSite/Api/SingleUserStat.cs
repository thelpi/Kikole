﻿using System;

namespace KikoleSite.Api
{
    public class SingleUserStat
    {
        public DateTime Date { get; set; }

        public string Answer { get; set; }

        public bool Attempt { get; set; }

        public TimeSpan? Time { get; set; }

        public int? Points { get; set; }

        public int? TimePosition { get; set; }

        public int? PointsPosition { get; set; }
    }
}
