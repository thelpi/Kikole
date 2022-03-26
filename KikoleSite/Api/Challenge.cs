namespace KikoleSite.Api
{
    public class Challenge
    {
        public ulong Id { get; set; }

        public string OpponentLogin { get; set; }

        public byte PointsRate { get; set; }

        public bool? IsAccepted { get; set; }
    }
}
