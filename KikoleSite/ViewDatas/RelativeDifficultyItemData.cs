using System;
using KikoleSite.Enums;

namespace KikoleSite.ViewDatas
{
    public class RelativeDifficultyItemData
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public string PlayerColor { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public int DupesOrBetter { get; set; }
        public int RelativeDifficulty { get; set; }
    }
}
