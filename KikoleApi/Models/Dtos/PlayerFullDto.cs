using System.Collections.Generic;

namespace KikoleApi.Models.Dtos
{
    public class PlayerFullDto
    {
        internal PlayerDto Player { get; set; }

        internal IReadOnlyList<PlayerClubDto> PlayerClubs { get; set; }

        internal IReadOnlyList<ClubDto> Clubs { get; set; }
    }
}
