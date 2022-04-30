using System.Collections.Generic;

namespace KikoleApi.Models.Dtos
{
    public class PlayerFullDto
    {
        public PlayerDto Player { get; set; }

        public IReadOnlyList<PlayerClubDto> PlayerClubs { get; set; }

        public IReadOnlyList<ClubDto> Clubs { get; set; }
    }
}
