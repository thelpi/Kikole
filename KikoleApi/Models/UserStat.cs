using System;
using System.Collections.Generic;

namespace KikoleApi.Models
{
    public class UserStat
    {
        public string Login { get; set; }

        public int Attempts { get; set; }

        public int Successes { get; set; }

        public int TotalPoints { get; set; }

        public int? BestPoints { get; set; }

        public TimeSpan? AverageTime { get; set; }

        public TimeSpan? BestTime { get; set; }

        public IReadOnlyCollection<DailyUserStat> Stats { get; set; }
    }
}
