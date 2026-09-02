using System.Collections.Generic;

namespace KikoleSite.Models.Dtos
{
    public class PlayerFullDto
    {
        public required PlayerDto Player { get; set; }

        public required IReadOnlyList<PlayerClubDto> PlayerClubs { get; set; }

        public required IReadOnlyList<ClubDto> Clubs { get; set; }
    }
}
