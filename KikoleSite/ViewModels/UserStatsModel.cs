using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Helpers;
using KikoleSite.Models;

namespace KikoleSite.ViewModels
{
    public class UserStatsModel
    {
        public string Login { get; }

        public int Attempts { get; }

        public int AttemptsDayOne { get; }

        public int Successes { get; }

        public int SuccessesDayOne { get; }

        public int TotalPoints { get; }

        public int TotalPointsDayOne { get; }

        public string BestPoints { get; }

        public string BestPointsDayOne { get; }

        public string AverageTime { get; }

        public string AverageTimeDayOne { get; }

        public string BestTime { get; }

        public DateTime RegistrationDate { get; }

        public IReadOnlyCollection<SingleUserStatModel> Stats { get; }

        public IReadOnlyCollection<UserBadge> Badges { get; }

        public IReadOnlyCollection<Badge> MissingBadges { get; }

        public bool IsHimself { get; }

        public int Potentials => Stats.Count(_ => !_.IsCreator);

        public int AttemptDayOneRate => GetRate(AttemptsDayOne, Potentials);

        public int AttemptRate => GetRate(Attempts, Potentials);

        public int SuccessDayOneRate => GetRate(SuccessesDayOne, AttemptsDayOne);

        public int SuccessRate => GetRate(Successes, Attempts);

        public int PointsPerAttemptDayOne => GetRate(TotalPointsDayOne, AttemptsDayOne, 1);

        public int PointsPerAttempt => GetRate(TotalPoints, Attempts, 1);


        public UserStatsModel(UserStat apiStat,
            IReadOnlyCollection<UserBadge> badges,
            IReadOnlyCollection<Badge> allBadges,
            IReadOnlyCollection<string> knownAnswers,
            bool isHimself)
        {
            Login = apiStat.Login;
            Attempts = apiStat.Attempts;
            AttemptsDayOne = apiStat.AttemptsDayOne;
            Successes = apiStat.Successes;
            SuccessesDayOne = apiStat.SuccessesDayOne;
            TotalPoints = apiStat.TotalPoints;
            TotalPointsDayOne = apiStat.TotalPointsDayOne;
            BestPoints = apiStat.BestPoints.ToNaString();
            BestPointsDayOne = apiStat.BestPointsDayOne.ToNaString();
            AverageTime = apiStat.AverageTime.ToNaString();
            AverageTimeDayOne = apiStat.AverageTimeDayOne.ToNaString();
            BestTime = apiStat.BestTime.ToNaString();
            Stats = apiStat.Stats
                .Select(s => new SingleUserStatModel(s, knownAnswers.Contains(s.Answer)))
                .ToList();
            Badges = badges;
            MissingBadges = allBadges
                .Where(b => !badges.Any(_ => _.Id == b.Id))
                .ToList();
            IsHimself = isHimself;
            RegistrationDate = apiStat.RegistrationDate;
        }

        private int GetRate(int a, int b, int c = 100)
        {
            return b == 0 ? 0 : (int)Math.Round(a / (decimal)b * c);
        }
    }
}
