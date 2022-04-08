namespace KikoleSite.Api
{
    public class Badge
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Users { get; set; }

        public bool Hidden { get; set; }

        public bool Unique { get; set; }

        public ulong? SubBadgeId { get; set; }
    }
}
