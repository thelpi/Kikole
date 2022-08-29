using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.ViewDatas
{
    public class ChronologyCanvasItemData
    {
        public string Label { get; set; }
        public string Color { get; set; }
        public int Days { get; set; }
        public int DaysBefore { get; set; }
        public double Opacity { get; set; }
        public Stage Stage { get; set; }
        public Level? Level { get; set; }
    }
}
