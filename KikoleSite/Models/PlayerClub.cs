using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Models;

public class PlayerClub
{
    public byte HistoryPosition { get; }

    public string Name { get; }

    public bool IsLoan { get; }

    internal PlayerClub(PlayerClubDto playerClub, IEnumerable<ClubDto> clubs)
    {
        // le club de la carriere doit figurer dans le referentiel fourni ; son absence
        // est une incoherence de donnees, pas un cas a degrader
        var club = clubs.SingleOrDefault(c => c.Id == playerClub.ClubId)
            ?? throw new InvalidOperationException(
                $"Le club {playerClub.ClubId} de la carriere du joueur {playerClub.PlayerId} est absent du referentiel.");

        Name = club.Name;
        HistoryPosition = playerClub.HistoryPosition;
        IsLoan = playerClub.IsLoan > 0;
    }
}
