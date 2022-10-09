using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using KikoleSite.Models;
using KikoleSite.Models.Enums;

namespace KikoleSite.ViewModels
{
    public class LeaderboardModel
    {
        public IReadOnlyCollection<LeaderSorts> SortTypes { get; }
            = Enum.GetValues(typeof(LeaderSorts))
                .Cast<LeaderSorts>()
                .ToList();

        public IReadOnlyCollection<DayLeaderSorts> DaySortTypes { get; }
            = Enum.GetValues(typeof(DayLeaderSorts))
                .Cast<DayLeaderSorts>()
                .ToList();

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime MinimalDate { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime MaximalDate { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime LeaderboardDay { get; set; }

        public LeaderSorts SortType { get; set; }

        public DayLeaderSorts DaySortType { get; set; }

        public Dayboard Dayboard { get; set; }
    }
}
