using System;
using System.Collections.Generic;

namespace KikoleApi.Models
{
    public class UserStats
    {
        public string Login { get; set; }

        public int TriedCount { get; set; }

        public int SuccessFulCount { get; set; }

        public int TotalPoints { get; set; }

        public TimeSpan AverageTime { get; set; }

        public TimeSpan BestTime { get; set; }

        public IReadOnlyCollection<SingleUserStat> Stats { get; set; }
    }
}
