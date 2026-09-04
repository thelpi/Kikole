using System;
using System.Collections.Generic;

namespace KikoleSite.Models.Statistics;

public class ActiveUsers
{
    public required IReadOnlyDictionary<(int y, int m), int> MonthlyDatas { get; set; }

    public required IReadOnlyDictionary<(int y, int w), int> WeeklyDatas { get; set; }

    public required IReadOnlyDictionary<DateTime, int> DailyDatas { get; set; }
}
