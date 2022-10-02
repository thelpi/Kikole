using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Helpers;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
{
    public class Dayboard
    {
        public DateTime Date { get; set; }
        public DayLeaderSorts Sort { get; set; }
        public IReadOnlyCollection<DayboardLeaderItem> Leaders { get; set; }
        public IReadOnlyCollection<DayboardSearcherItem> Searchers { get; set; }
        public int DayAttemps => Searchers.Count(_ => _.Date == Date) + DaySuccess;
        public int TotalAttemps => Searchers.Count + TotalSuccess;
        public int DaySuccess => Leaders.Count(_ => _.Date == Date && !_.IsCreator);
        public int TotalSuccess => Leaders.Count(_ => !_.IsCreator);
        public int DaySuccessRate => DaySuccess.ToPercentRate(DayAttemps);
        public int TotalSuccessRate => TotalSuccess.ToPercentRate(TotalAttemps);
    }
}
