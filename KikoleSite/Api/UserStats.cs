using System;
using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class UserStats
    {
        public string Login { get; set; }

        public bool TriedCount { get; set; }

        public bool SuccessFulCount { get; set; }

        public int TotalPoints { get; set; }

        public TimeSpan AverageTime { get; set; }

        public TimeSpan BestTime { get; set; }

        public IReadOnlyCollection<SingleUserStat> Stats { get; set; }
    }
}
