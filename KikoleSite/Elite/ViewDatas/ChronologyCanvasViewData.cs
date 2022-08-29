using System.Collections.Generic;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.ViewDatas
{
    public class ChronologyCanvasViewData
    {
        public const int TotalWidth = 1600;

        public IReadOnlyDictionary<Stage, string> StageImages { get; set; }
        public Game Game { get; set; }
        public ChronologyTypeItemData ChronologyType { get; set; }
        public Engine? Engine { get; set; }
        public int TotalDays { get; set; }
        public long? PlayerId { get; set; }
        public bool Anonymise { get; set; }

        public bool IsFullStage => ChronologyType.IsFullStage();
    }
}
