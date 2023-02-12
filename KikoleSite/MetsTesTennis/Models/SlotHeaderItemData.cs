using KikoleSite.MetsTesTennis.Enums;

namespace KikoleSite.MetsTesTennis.Models
{
    public class SlotHeaderItemData
    {
        public string MainTournamentName { get; set; }

        public ushort SlotId { get; set; }

        public ulong MainTournamentId { get; set; }

        public Levels SlotLevel { get; set; }
    }
}
