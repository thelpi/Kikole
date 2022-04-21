namespace KikoleSite.Api
{
    public class PlayerClubRequest
    {
        public byte HistoryPosition { get; set; }

        public bool IsLoan { get; set; }

        public ulong ClubId { get; set; }
    }
}
