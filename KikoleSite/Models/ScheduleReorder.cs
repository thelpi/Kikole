using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Models;

public enum ScheduleReorderResult
{
    Saved,
    OutOfDate
}

public static class ScheduleSnapshot
{
    /// <summary>Empreinte (id et date) des kikoles programmes : change des que la liste evolue.</summary>
    public static string From(IEnumerable<PlayerDto> scheduledPlayers)
    {
        return string.Join(",", scheduledPlayers.Select(p => $"{p.Id}:{p.PublicationDate:yyyyMMdd}"));
    }
}