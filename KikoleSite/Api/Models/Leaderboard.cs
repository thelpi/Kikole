using System;
using System.Collections.Generic;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models
{
    public class Leaderboard
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public LeaderSorts Sort { get; set; }
        public IReadOnlyCollection<LeaderboardItem> Items { get; set; }
    }
}
