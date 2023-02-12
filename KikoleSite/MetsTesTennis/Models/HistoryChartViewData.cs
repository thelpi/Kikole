using System.Collections.Generic;

namespace KikoleSite.MetsTesTennis.Models
{
    public class HistoryChartViewData
    {
        // assumes ordered by position
        public IReadOnlyList<SlotHeaderItemData> SlotHeaders { get; set; }

        // assumes ordered by year
        public IReadOnlyList<YearChartItemData> YearCharts { get; set; }
    }
}
