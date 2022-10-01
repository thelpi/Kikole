using System;
using System.Collections.Generic;

namespace KikoleSite.Models
{
    public class PalmaresModel
    {
        public IReadOnlyCollection<(DateTime date, (ulong playerId, string playerName)[] palmares)> MonthlyPalmares { get; set; }

        public IReadOnlyCollection<(string playerName, int firstPlaces, int secondPlaces, int thirdPlaces)> GlobalPalmares { get; set; }
    }
}
