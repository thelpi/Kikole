using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Models
{
    public class PlayerClub
    {
        public byte HistoryPosition { get; set; }

        public string Name { get; set; }

        public bool IsLoan { get; set; }

        internal PlayerClub(PlayerClubDto playerClub, IEnumerable<ClubDto> clubs)
        {
            Name = clubs.Single(c => c.Id == playerClub.ClubId).Name;
            HistoryPosition = playerClub.HistoryPosition;
            IsLoan = playerClub.IsLoan > 0;
        }

        // mandatory for json
        public PlayerClub() { }
    }
}
