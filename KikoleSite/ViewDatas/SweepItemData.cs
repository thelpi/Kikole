using System;
using KikoleSite.Enums;

namespace KikoleSite.ViewDatas
{
    public class SweepItemData
    {
        public Stage Stage { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
        public int Days { get; set; }
    }
}
