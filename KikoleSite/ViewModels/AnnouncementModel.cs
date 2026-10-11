using System;

namespace KikoleSite.ViewModels;

public class AnnouncementModel
{
    /// <summary>0 pour une nouvelle annonce.</summary>
    public ulong Id { get; set; }

    public string? Message { get; set; }

    public DateTime? DisplayFrom { get; set; }

    public DateTime? DisplayTo { get; set; }

    public string? ErrorMessage { get; set; }
}