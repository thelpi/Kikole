﻿using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;

namespace KikoleApi.Models
{
    public class UserStat
    {
        public string Login { get; }

        public int Attempts { get; }

        public int Successes { get; }

        public int TotalPoints { get; }

        public int? BestPoints { get; }

        public TimeSpan? AverageTime { get; }

        public TimeSpan? BestTime { get; }

        public IReadOnlyCollection<DailyUserStat> Stats { get; }

        internal UserStat(IReadOnlyCollection<DailyUserStat> stats, string login)
        {
            Attempts = stats.Count(s => s.Attempt);
            AverageTime = stats.Where(s => s.Time.HasValue).Select(s => s.Time.Value).Average();
            BestPoints = stats.Any(s => s.Points.HasValue)
                ? stats.Where(s => s.Points.HasValue).Max(s => s.Points.Value)
                : default(int?);
            BestTime = stats.Any(s => s.Time.HasValue)
                ? stats.Where(s => s.Time.HasValue).Min(s => s.Time.Value)
                : default(TimeSpan?);
            Login = login;
            Stats = stats;
            Successes = stats.Count(s => s.Points.HasValue);
            TotalPoints = stats.Sum(s => s.Points.GetValueOrDefault(0));
        }
    }
}
