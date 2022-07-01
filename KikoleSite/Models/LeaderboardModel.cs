using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Models
{
    public class LeaderboardModel
    {
        [DataType(DataType.Date, ErrorMessage = "Date only")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime MinimalDate { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime MaximalDate { get; set; }

        public bool IncludePvp { get; set; }

        public string BoardName { get; set; }

        public LeaderSorts SortType { get; set; }

        public IReadOnlyCollection<Leader> TodayLeaders { get; set; }
        public IReadOnlyCollection<(ulong, string)> Searchers { get; set; }

        public int TodayAttemps { get; set; }
        public int TotalAttemps { get; set; }
        public int TotalSuccessRate { get; set; }
        public int TodaySuccessRate { get; set; }

        public IReadOnlyCollection<LeaderSorts> SortTypes { get; }
            = Enum.GetValues(typeof(LeaderSorts))
                .Cast<LeaderSorts>()
                .ToList();

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime LeaderboardDay { get; set; }

        public DayLeaderSorts DaySortType { get; set; }

        public IReadOnlyCollection<DayLeaderSorts> DaySortTypes { get; }
            = Enum.GetValues(typeof(DayLeaderSorts))
                .Cast<DayLeaderSorts>()
                .ToList();

        public Awards Awards { get; set; }

        public Leaderboard Leaderboard { get; set; }
    }
}
