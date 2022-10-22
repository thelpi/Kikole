using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Helpers;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
{
    public class Dayboard
    {
        public bool Hidden { get; set; }
        public DateTime Date { get; set; }
        public DayLeaderSorts Sort { get; set; }
        public IReadOnlyCollection<DayboardLeaderItem> Leaders { get; set; }
        public IReadOnlyCollection<DayboardSearcherItem> Searchers { get; set; }
        public int DayAttemps => Searchers?.Count(_ => _.Date == Date) ?? 0 + DaySuccess;
        public int TotalAttemps => Searchers?.Count ?? 0 + TotalSuccess;
        public int DaySuccess => Leaders?.Count(_ => _.Date == Date && !_.IsCreator) ?? 0;
        public int TotalSuccess => Leaders?.Count(_ => !_.IsCreator) ?? 0;
        public int DaySuccessRate => DaySuccess.ToPercentRate(DayAttemps);
        public int TotalSuccessRate => TotalSuccess.ToPercentRate(TotalAttemps);
    }
}
