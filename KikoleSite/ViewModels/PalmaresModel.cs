using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models;

namespace KikoleSite.ViewModels
{
    public class PalmaresModel
    {
        public IReadOnlyCollection<(DateTime date, (ulong playerId, string playerName)[] palmares)> MonthlyPalmares { get; set; }

        public IReadOnlyCollection<(string playerName, int firstPlaces, int secondPlaces, int thirdPlaces)> GlobalPalmares { get; set; }

        public PalmaresModel(Palmares data)
        {
            MonthlyPalmares = data.MonthlyPalmares
                .Select(x => (
                    new DateTime(x.Key.year, x.Key.month, 1),
                    new[]
                    {
                        (x.Value.first.Id, x.Value.first.Login),
                        (x.Value.second.Id, x.Value.second.Login),
                        (x.Value.third.Id, x.Value.third.Login)
                    }))
                .ToList();
            GlobalPalmares = data.GlobalPalmares
                .Select(x => (x.user.Login, x.first, x.second, x.third))
                .ToList();
        }
    }
}
