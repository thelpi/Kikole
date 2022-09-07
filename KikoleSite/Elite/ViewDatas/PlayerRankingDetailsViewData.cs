using System;
using System.Collections.Generic;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.ViewDatas
{
    public class PlayerRankingDetailsViewData
    {
        public string PlayerName { get; set; }
        public Game Game { get; set; }
        public int OverallPoints { get; set; }
        public int OverallRanking { get; set; }
        public TimeSpan OverallTime { get; set; }
        public int EasyPoints { get; set; }
        public TimeSpan EasyTime { get; set; }
        public int MediumPoints { get; set; }
        public TimeSpan MediumTime { get; set; }
        public int HardPoints { get; set; }
        public TimeSpan HardTime { get; set; }
        public IReadOnlyCollection<PlayerStageDetailsItemData> DetailsByStage { get; set; }
    }
}
