using System;
using System.Collections.Generic;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models;

public class Dayboard
{
    public bool Hidden { get; set; }

    /// <summary>
    /// Vrai si l'utilisateur consultant ce tableau a le droit de voir le detail des
    /// propositions d'un autre utilisateur pour <see cref="Date"/> (trouve/cree/admin) -
    /// distinct du seul fait de pouvoir voir le tableau (ex. classement achete sans
    /// avoir trouve : le tableau est visible, mais pas le detail par utilisateur).
    /// </summary>
    public bool CanViewDetails { get; set; }

    public DateOnly Date { get; set; }
    public DayLeaderSorts Sort { get; set; }
    public required IReadOnlyCollection<DayboardLeaderItem> Leaders { get; set; }
    public required IReadOnlyCollection<DayboardSearcherItem> Searchers { get; set; }
}
