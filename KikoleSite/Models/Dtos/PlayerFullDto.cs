using System.Collections.Generic;

namespace KikoleSite.Models.Dtos;

public record PlayerFullDto
{
    public required PlayerDto Player { get; init; }

    public required IReadOnlyList<PlayerClubDto> PlayerClubs { get; init; }

    public required IReadOnlyList<ClubDto> Clubs { get; init; }
}
