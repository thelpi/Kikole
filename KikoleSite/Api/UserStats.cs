using System;
using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class UserStats
    {
        public string Login { get; set; }

        public int Attempts { get; set; }

        public int Successes { get; set; }

        public int TotalPoints { get; set; }

        public int? BestPoints { get; set; }

        public TimeSpan? AverageTime => AverageTimeSec.HasValue ? new TimeSpan(0, 0, AverageTimeSec.Value) : default(TimeSpan?);

        public int? AverageTimeSec { get; set; }

        public TimeSpan? BestTime => BestTimeSec.HasValue ? new TimeSpan(0, 0, BestTimeSec.Value) : default(TimeSpan?);

        public int? BestTimeSec { get; set; }

        public IReadOnlyCollection<SingleUserStat> Stats { get; set; }
    }
}
