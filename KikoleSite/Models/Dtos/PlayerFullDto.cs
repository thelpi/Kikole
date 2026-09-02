using System.Collections.Generic;

namespace KikoleSite.Models.Dtos
{
    public class PlayerFullDto
    {
        public PlayerDto Player { get; set; } = null!;

        public IReadOnlyList<PlayerClubDto> PlayerClubs { get; set; } = null!;

        public IReadOnlyList<ClubDto> Clubs { get; set; } = null!;
    }
}
