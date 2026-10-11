using System;
using System.Collections.Generic;

namespace KikoleSite.ViewModels;

public enum AnnouncementStatus
{
    Displayed,
    Superseded,
    Scheduled,
    Expired
}

public record AnnouncementRow(
    ulong Id,
    string Message,
    DateTime? DisplayFrom,
    DateTime? DisplayTo,
    AnnouncementStatus Status);

public class AnnouncementsModel
{
    public string? Feedback { get; set; }

    public IReadOnlyList<AnnouncementRow> Rows { get; set; } = [];
}