using System.Collections.Generic;
using KikoleSite.Elite.Dtos;

namespace KikoleSite.Elite.Models.Integration
{
    public class RefreshPlayersResult
    {
        public IReadOnlyCollection<PlayerDto> CreatedPlayers { get; set; }
        public IReadOnlyCollection<PlayerDto> BannedPlayers { get; set; }
        public IReadOnlyCollection<string> Errors { get; set; }
    }
}
