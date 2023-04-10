using System;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.ViewDatas
{
    public class SweepItemData
    {
        public Stage Stage { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
        public int Days { get; set; }
    }
}
