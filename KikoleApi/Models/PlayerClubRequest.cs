namespace KikoleApi.Models
{
    public class PlayerClubRequest
    {
        public ulong ClubId { get; set; }

        public byte HistoryPosition { get; set; }

        public byte ImportancePosition { get; set; }

        internal bool IsValid()
        {
            return ClubId >= 0
                && HistoryPosition > 0
                && ImportancePosition > 0;
        }
    }
}
