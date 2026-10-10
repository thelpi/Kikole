using System.Collections.Generic;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Models;

public record PlayerEditData(
    PlayerDto Player,
    IReadOnlyList<PlayerClubDto> Clubs,
    string? ClueFr,
    string? EasyClueFr);
