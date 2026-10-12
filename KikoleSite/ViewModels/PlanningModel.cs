using System;
using System.Collections.Generic;

namespace KikoleSite.ViewModels;

public record PlanningRow(ulong Id, string Name);

public class PlanningModel
{
    public string? Feedback { get; set; }

    public string? Error { get; set; }

    public DateOnly FirstDate { get; set; }

    public string Snapshot { get; set; } = string.Empty;

    public IReadOnlyList<PlanningRow> Players { get; set; } = [];
}

public class PlanningSaveModel
{
    /// <summary>Identifiants dans l'ordre choisi (le premier passe a demain).</summary>
    public List<ulong> Ids { get; set; } = [];

    public string? Snapshot { get; set; }
}