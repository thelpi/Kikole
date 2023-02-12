using KikoleSite.MetsTesTennis.Enums;

namespace KikoleSite.MetsTesTennis.Models
{
    public class EditionChartItemData
    {
        public ulong WinnerId { get; set; }

        public string WinnerName { get; set; }

        public Surfaces? Surface { get; set; }

        public bool Indoor { get; set; }

        public string TournamentName { get; set; }

        public ulong TournamentId { get; set; }

        public ushort SlotId { get; set; }
    }
}
