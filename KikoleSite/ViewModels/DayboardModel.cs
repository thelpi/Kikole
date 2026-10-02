using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Helpers;
using KikoleSite.Models;
using KikoleSite.Models.Enums;

namespace KikoleSite.ViewModels;

/// <summary>
/// Classement du jour tel qu'affiche, rendu par la vue ou renvoye en JSON a
/// <c>site.js</c> : seul le temps des trouveurs est formate (cf. <see cref="DayboardLeaderRow"/>),
/// le reste reprend <see cref="Dayboard"/> tel quel.
/// </summary>
public class DayboardModel
{
    public bool Hidden { get; init; }
    public bool CanViewDetails { get; init; }
    public DateOnly Date { get; init; }
    public DayLeaderSorts Sort { get; init; }
    public required IReadOnlyCollection<DayboardLeaderRow> Leaders { get; init; }
    public required IReadOnlyCollection<DayboardSearcherItem> Searchers { get; init; }

    public static DayboardModel From(Dayboard dayboard) => new()
    {
        Hidden = dayboard.Hidden,
        CanViewDetails = dayboard.CanViewDetails,
        Date = dayboard.Date,
        Sort = dayboard.Sort,
        Leaders = dayboard.Leaders.Select(DayboardLeaderRow.From).ToList(),
        Searchers = dayboard.Searchers
    };
}

public class DayboardLeaderRow
{
    public int Rank { get; init; }
    public ulong UserId { get; init; }
    public required string UserName { get; init; }
    public int Points { get; init; }
    public bool IsCreator { get; init; }
    public required string TimeString { get; init; }

    public static DayboardLeaderRow From(DayboardLeaderItem item) => new()
    {
        Rank = item.Rank,
        UserId = item.UserId,
        UserName = item.UserName,
        Points = item.Points,
        IsCreator = item.IsCreator,
        TimeString = item.Time.ToNaString()
    };
}
