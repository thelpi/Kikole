namespace KikoleApi.Domain.Models.Dtos
{
    internal class PlayerClubDto
    {
        public long PlayerId { get; set; }

        public string Name { get; set; }

        public byte HistoryPosition { get; set; }

        public byte ImportancePosition { get; set; }

        public string AllowedNames { get; set; }
    }
}
