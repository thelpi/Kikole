using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Helpers;

namespace KikoleSite.Api.Models
{
    public class UserStat
    {
        public string Login { get; }

        public int Attempts { get; }

        public int AttemptsDayOne { get; }

        public int Successes { get; }

        public int SuccessesDayOne { get; }

        public int TotalPoints { get; }

        public int TotalPointsDayOne { get; }

        public int? BestPoints { get; }

        public int? BestPointsDayOne { get; }

        public TimeSpan? AverageTime { get; }

        public TimeSpan? AverageTimeDayOne { get; }

        public TimeSpan? BestTime { get; }

        public IReadOnlyCollection<DailyUserStat> Stats { get; }

        public DateTime RegistrationDate { get; }

        internal UserStat(IReadOnlyCollection<DailyUserStat> stats, string login, DateTime registrationDate)
        {
            Login = login;
            Stats = stats;
            RegistrationDate = registrationDate;

            // player creation NOT included
            Attempts = stats.Count(s => s.Attempt);
            AttemptsDayOne = stats.Count(s => s.AttemptDayOne);
            AverageTime = stats.Where(s => s.Time.HasValue).Select(s => s.Time.Value).Average();
            AverageTimeDayOne = stats.Where(s => s.Time.HasValue && s.SuccessDayOne).Select(s => s.Time.Value).Average();
            BestTime = stats.Any(s => s.Time.HasValue)
                ? stats.Where(s => s.Time.HasValue).Min(s => s.Time.Value)
                : default(TimeSpan?);
            BestPoints = stats.Any(s => s.Points.HasValue && s.Attempt)
                ? stats.Where(s => s.Points.HasValue && s.Attempt).Max(s => s.Points.Value)
                : default(int?);
            BestPointsDayOne = stats.Any(s => s.Points.HasValue && s.SuccessDayOne && s.AttemptDayOne)
                ? stats.Where(s => s.Points.HasValue && s.SuccessDayOne && s.AttemptDayOne).Max(s => s.Points.Value)
                : default(int?);
            Successes = stats.Count(s => s.Points.HasValue && s.Attempt);
            SuccessesDayOne = stats.Count(s => s.Points.HasValue && s.AttemptDayOne);

            // player creation included
            TotalPoints = stats.Sum(s => s.Points.GetValueOrDefault(0));
            TotalPointsDayOne = stats.Where(s => s.SuccessDayOne).Sum(s => s.Points.GetValueOrDefault(0));
        }
    }
}
