using System;
using System.Collections.Generic;

namespace KikoleSite.Elite.ViewDatas
{
    public class SimulatedRankingViewData
    {
        public IReadOnlyCollection<TimeRankingItemData> TimeRankingEntries { get; set; }
        public IReadOnlyCollection<PointsRankingItemData> PointsRankingEntries { get; set; }
        public IReadOnlyCollection<StageWorldRecordItemData> StageWorldRecordEntries { get; set; }
        public TimeSpan CombinedTime { get; set; }
        public TimeSpan EasyCombinedTime { get; set; }
        public TimeSpan MediumCombinedTime { get; set; }
        public TimeSpan HardCombinedTime { get; set; }
        public string EasyLabel { get; set; }
        public string MediumLabel { get; set; }
        public string HardLabel { get; set; }
        public string EasyShortLabel { get; set; }
        public string MediumShortLabel { get; set; }
        public string HardShortLabel { get; set; }
    }
}
