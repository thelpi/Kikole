namespace KikoleSite.Models.Dtos
{
    public class PlayerClubDto
    {
        public ulong PlayerId { get; set; }

        public ulong ClubId { get; set; }

        public byte HistoryPosition { get; set; }

        public byte IsLoan { get; set; }
    }
}
