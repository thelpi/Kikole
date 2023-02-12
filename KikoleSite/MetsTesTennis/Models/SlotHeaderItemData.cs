using System.Collections.Generic;
using KikoleSite.MetsTesTennis.Enums;

namespace KikoleSite.MetsTesTennis.Models
{
    public class SlotHeaderItemData
    {
        public IReadOnlyList<(ulong, string)> Tournaments { get; set; }

        public ushort SlotId { get; set; }

        public Levels SlotLevel { get; set; }
    }
}
