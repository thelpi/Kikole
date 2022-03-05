using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class PlayerClub
    {
        public byte HistoryPosition { get; }

        public byte ImportancePosition { get; }

        public string Name { get; }

        internal PlayerClub(ClubDto club, PlayerClubDto playerClub)
        {
            HistoryPosition = playerClub.HistoryPosition;
            ImportancePosition = playerClub.ImportancePosition;
            Name = club.Name;
        }
    }
}
