using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using KikoleSite.Api;

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

        public LeaderSort SortType { get; set; }

        public IReadOnlyCollection<Leader> Leaders { get; set; }

        public IReadOnlyCollection<Leader> TodayLeaders { get; set; }

        public IReadOnlyCollection<LeaderSort> SortTypes { get; } = Enum.GetValues(typeof(LeaderSort)).Cast<LeaderSort>().ToList();

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime LeaderboardDay { get; set; }

        public LeaderSort DaySortType { get; set; }

        public IReadOnlyCollection<LeaderSort> DaySortTypes { get; } = Enum.GetValues(typeof(LeaderSort)).Cast<LeaderSort>().Where(ls => ls != LeaderSort.SuccessCount).ToList();

        public Awards Awards { get; set; }
    }
}
