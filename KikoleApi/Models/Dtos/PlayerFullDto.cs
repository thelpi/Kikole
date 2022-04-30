using System.Collections.Generic;

namespace KikoleApi.Models.Dtos
{
    public class PlayerFullDto
    {
        public PlayerDto Player { get; }

        public IReadOnlyList<PlayerClubDto> PlayerClubs { get; }

        public IReadOnlyList<ClubDto> Clubs { get; }

        internal PlayerFullDto(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            Player = player;
            PlayerClubs = playerClubs;
            Clubs = clubs;
        }
    }
}
