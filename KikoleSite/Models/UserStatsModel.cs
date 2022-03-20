﻿using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Models
{
    public class UserStatsModel
    {
        public string Login { get; set; }

        public int Attempts { get; set; }

        public int Successes { get; set; }

        public int TotalPoints { get; set; }

        public string BestPoints { get; set; }

        public string AverageTime { get; set; }

        public string BestTime { get; set; }

        public IReadOnlyCollection<SingleUserStatModel> Stats { get; set; }

        public IReadOnlyCollection<Api.UserBadge> Badges { get; set; }

        public IReadOnlyCollection<Api.Badge> MissingBadges { get; set; }

        public UserStatsModel(Api.UserStats apiStat,
            IReadOnlyCollection<Api.UserBadge> badges,
            IReadOnlyCollection<Api.Badge> allBadges)
        {
            Login = apiStat.Login;
            Attempts = apiStat.Attempts;
            Successes = apiStat.Successes;
            TotalPoints = apiStat.TotalPoints;
            BestPoints = apiStat.BestPoints?.ToString() ?? "N/A";
            AverageTime = apiStat.AverageTime?.ToString(@"hh\:mm") ?? "N/A";
            BestTime = apiStat.BestTime?.ToString(@"hh\:mm") ?? "N/A";
            Stats = apiStat.Stats.Select(s => new SingleUserStatModel(s)).ToList();
            Badges = badges;
            MissingBadges = allBadges.Where(b => !badges.Any(_ => _.Id == b.Id)).ToList();
        }
    }
}
