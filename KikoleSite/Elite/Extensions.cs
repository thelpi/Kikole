using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Elite
{
    public static class Extensions
    {
        private const int GoldeneyeStagesCount = 20;

        private static readonly IReadOnlyDictionary<string, ControlStyle> _controlStyleConverters = new Dictionary<string, ControlStyle>
        {
            { "1.1", ControlStyle.OnePointOne },
            { "1.2", ControlStyle.OnePointTwo },
            { "1.3", ControlStyle.OnePointThree },
            { "1.4", ControlStyle.OnePointFour },
            { "2.1", ControlStyle.TwoPointOne },
            { "2.2", ControlStyle.TwoPointTwo },
            { "2.3", ControlStyle.TwoPointThree },
            { "2.4", ControlStyle.TwoPointFour }
        };
        private static readonly Dictionary<Game, DateTime> _eliteBeginDate = new Dictionary<Game, DateTime>
        {
            { Game.GoldenEye, new DateTime(1998, 05, 14) },
            { Game.PerfectDark, new DateTime(2000, 06, 06) }
        };

        public static ControlStyle? ToControlStyle(string controlStyleLabel)
        {
            return controlStyleLabel != null && _controlStyleConverters.ContainsKey(controlStyleLabel) ?
                _controlStyleConverters[controlStyleLabel] : default(ControlStyle?);
        }

        public static IReadOnlyCollection<Stage> GetStages(this Game game)
        {
            if (game == Game.GoldenEye)
            {
                return SystemExtensions.Enumerate<Stage>()
                    .Where(s => (int)s <= GoldeneyeStagesCount)
                    .ToList();
            }
            else
            {
                return SystemExtensions.Enumerate<Stage>()
                    .Where(s => (int)s > GoldeneyeStagesCount)
                    .ToList();
            }
        }

        public static DateTime GetEliteFirstDate(this Game game)
        {
            return _eliteBeginDate[game];
        }

        public static Game GetGame(this Stage stage)
        {
            return ((int)stage) <= GoldeneyeStagesCount
                ? Game.GoldenEye
                : Game.PerfectDark;
        }
    }
}
