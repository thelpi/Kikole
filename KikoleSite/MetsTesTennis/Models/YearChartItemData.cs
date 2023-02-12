using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.MetsTesTennis.Models
{
    public class YearChartItemData
    {
        public IReadOnlyList<EditionChartItemData> EditionCharts { get; set; }

        public int Year { get; set; }

        public EditionChartItemData GetSlotEdition(SlotHeaderItemData slotHeader)
        {
            return EditionCharts.FirstOrDefault(ec => ec.SlotId == slotHeader.SlotId);
        }
    }
}
