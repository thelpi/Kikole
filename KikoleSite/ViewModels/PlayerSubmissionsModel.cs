using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.ViewModels;

public class PlayerSubmissionsModel
{
    public IReadOnlyCollection<PlayerSubmissionModel> Players { get; set; } = [];

    public ulong SelectedId { get; set; }

    public PlayerSubmissionModel? SelectedPlayer => Players.SingleOrDefault(p => p.Id == SelectedId);

    public string? ErrorMessage { get; set; }

    public string? InfoMessage { get; set; }

    public string? ClueOverwriteEn { get; set; }

    public string? ClueOverwriteFr { get; set; }

    public string? EasyClueOverwriteEn { get; set; }

    public string? EasyClueOverwriteFr { get; set; }

    public string? RefusalReason { get; set; }

    /// <summary>
    /// Force la date de publication au lieu du "bout de chaine" habituel (uniquement pris
    /// en compte cote controleur pour une acceptation). Format "yyyy-MM-dd" (input HTML
    /// <c>type="date"</c>).
    /// </summary>
    public string? PublicationDate { get; set; }
}
