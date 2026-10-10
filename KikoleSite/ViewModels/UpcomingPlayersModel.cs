using System;
using System.Collections.Generic;

namespace KikoleSite.ViewModels;

public record UpcomingPlayerRow(ulong Id, string Name, DateOnly? PublicationDate);

public class UpcomingPlayersModel
{
    public bool Saved { get; set; }

    public IReadOnlyList<UpcomingPlayerRow> Players { get; set; } = [];
}
