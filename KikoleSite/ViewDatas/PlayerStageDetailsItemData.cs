using System;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.ViewDatas
{
    public class PlayerStageDetailsItemData
    {
        public Stage Stage { get; set; }
        public string Image { get; set; }
        public int EasyPoints { get; set; }
        public int MediumPoints { get; set; }
        public int HardPoints { get; set; }
        public TimeSpan? EasyTime { get; set; }
        public TimeSpan? MediumTime { get; set; }
        public TimeSpan? HardTime { get; set; }
    }
}
