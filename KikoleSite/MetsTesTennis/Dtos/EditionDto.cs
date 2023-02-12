using System;

namespace KikoleSite.MetsTesTennis.Dtos
{
    public class EditionDto
    {
        public ulong Id { get; set; }
        public string Code { get; set; }
        public ulong TournamentId { get; set; }
        public string Name { get; set; }
        public ushort Year { get; set; }
        public byte? SurfaceId { get; set; }
        public ushort DrawSize { get; set; }
        public byte LevelId { get; set; }
        public DateTime Date { get; set; }
        public byte Indoor { get; set; }
        public ushort? SlotId { get; set; }
    }
}
