using System;
using System.Collections.Generic;

namespace KikoleSite.ViewModels;

public class UserDayModel
{
    public string? PlayerName { get; set; }
    public DateOnly ProposalDate { get; set; }
    public string? UserLogin { get; set; }
    public ulong UserId { get; set; }
    public int UserScore { get; set; }

    /// <summary>Jour joué précédent de ce joueur que le visiteur a le droit de consulter, s'il existe.</summary>
    public DateOnly? PreviousDate { get; set; }

    /// <summary>Jour joué suivant de ce joueur que le visiteur a le droit de consulter, s'il existe.</summary>
    public DateOnly? NextDate { get; set; }
    public IReadOnlyCollection<UserDayItemModel> ProposalDetails { get; set; } = [];
}
