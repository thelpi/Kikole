namespace KikoleSite.ViewModels;

public class ClubCreationModel
{
    public string? ErrorMessage { get; set; }

    public string? InfoMessage { get; set; }

    public string? MainNameEn { get; set; }

    public string? MainNameFr { get; set; }

    /// <summary>Un alias EN par ligne.</summary>
    public string? AlternativeNamesEn { get; set; }

    /// <summary>Un alias FR par ligne.</summary>
    public string? AlternativeNamesFr { get; set; }

    public string? Country { get; set; }

    public ulong Id { get; set; }
}
