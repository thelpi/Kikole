using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api;

namespace KikoleSite.Models
{
    public class UserStatsModel
    {
        public int SuccessRate => Attempts == 0
            ? 0
            : (int)Math.Round((Successes / (decimal)Attempts) * 100);

        public int PointsPerAttempt => Attempts == 0
            ? 0
            : (int)Math.Round(TotalPoints / (decimal)Attempts);

        public string Login { get; set; }

        public int Attempts { get; set; }

        public int Successes { get; set; }

        public int TotalPoints { get; set; }

        public string BestPoints { get; set; }

        public string AverageTime { get; set; }

        public string BestTime { get; set; }

        public IReadOnlyCollection<SingleUserStatModel> Stats { get; set; }

        public IReadOnlyCollection<UserBadge> Badges { get; set; }

        public IReadOnlyCollection<Badge> MissingBadges { get; set; }

        public bool IsHimself { get; set; }

        public UserStatsModel(UserStats apiStat,
            IReadOnlyCollection<UserBadge> badges,
            IReadOnlyCollection<Badge> allBadges,
            IReadOnlyCollection<string> knownAnswers,
            bool isHimself)
        {
            Login = apiStat.Login;
            Attempts = apiStat.Attempts;
            Successes = apiStat.Successes;
            TotalPoints = apiStat.TotalPoints;
            BestPoints = apiStat.BestPoints.ToNaString();
            AverageTime = apiStat.AverageTime.ToNaString();
            BestTime = apiStat.BestTime.ToNaString();
            Stats = apiStat.Stats
                .Select(s => new SingleUserStatModel(s, knownAnswers.Contains(s.Answer)))
                .ToList();
            Badges = badges;
            MissingBadges = allBadges
                .Where(b => !badges.Any(_ => _.Id == b.Id))
                .ToList();
            IsHimself = isHimself;
        }
    }
}
