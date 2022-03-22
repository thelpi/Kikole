using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class PlayerClub
    {
        public byte HistoryPosition { get; }

        public string Name { get; }

        internal PlayerClub(ClubDto club, IEnumerable<PlayerClubDto> playerClubs)
        {
            Name = club.Name;
            HistoryPosition = playerClubs
                .First(pc => pc.ClubId == club.Id)
                .HistoryPosition;
        }
    }
}
