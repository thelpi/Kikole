using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class PlayerClub
    {
        public byte HistoryPosition { get; }

        public string Name { get; }

        internal PlayerClub(ClubDto club, PlayerClubDto playerClub)
        {
            HistoryPosition = playerClub.HistoryPosition;
            Name = club.Name;
        }
    }
}
