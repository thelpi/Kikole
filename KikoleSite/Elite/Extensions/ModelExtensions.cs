using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Models;

namespace KikoleSite.Elite.Extensions
{
    public static class ModelExtensions
    {
        private const int GoldeneyeStagesCount = 20;

        public const string DefaultLabel = "Unknown";

        public const string PerfectDarkDuelStageFormatedName = "duel";

        private static readonly Dictionary<Game, string> _eliteUrlName = new Dictionary<Game, string>
        {
            { Game.GoldenEye, "goldeneye" },
            { Game.PerfectDark, "perfect-dark" }
        };
        private static readonly Dictionary<Stage, string> _stageLabels = new Dictionary<Stage, string>
        {
            { Stage.Dam, "Dam" },
            { Stage.Facility, "Facility" },
            { Stage.Runway, "Runway" },
            { Stage.Surface1, "Surface 1" },
            { Stage.Bunker1, "Bunker 1" },
            { Stage.Silo, "Silo" },
            { Stage.Frigate, "Frigate" },
            { Stage.Surface2, "Surface 2" },
            { Stage.Bunker2, "Bunker 2" },
            { Stage.Statue, "Statue" },
            { Stage.Archives, "Archives" },
            { Stage.Streets, "Streets" },
            { Stage.Depot, "Depot" },
            { Stage.Train, "Train" },
            { Stage.Jungle, "Jungle" },
            { Stage.Control, "Control" },
            { Stage.Caverns, "Caverns" },
            { Stage.Cradle, "Cradle" },
            { Stage.Aztec, "Aztec" },
            { Stage.Egypt, "Egypt" },
            { Stage.Defection, "dataDyne Central - Defection" },
            { Stage.Investigation, "dataDyne Research - Investigation" },
            { Stage.Extraction, "dataDyne Central - Extraction" },
            { Stage.Villa, "Carrington Villa - Hostage One" },
            { Stage.Chicago, "Chicago - Stealth" },
            { Stage.G5, "G5 Building - Reconnaissance" },
            { Stage.Infiltration, "Area 51 - Infiltration" },
            { Stage.Rescue, "Area 51 - Rescue" },
            { Stage.Escape, "Area 51 - Escape" },
            { Stage.AirBase, "Air Base - Espionage" },
            { Stage.AirForceOne, "Air Force One - Antiterrorism" },
            { Stage.CrashSite, "Crash Site - Confrontation" },
            { Stage.PelagicII, "Pelagic II - Exploration" },
            { Stage.DeepSea, "Deep Sea - Nullify Threat" },
            { Stage.CI, "Carrington Institute - Defense" },
            { Stage.AttackShip, "Attack Ship - Covert Assault" },
            { Stage.SkedarRuins, "Skedar Ruins - Battle Shrine" },
            { Stage.MBR, "Mr. Blonde's Revenge" },
            { Stage.MaianSOS, "Maian SOS" },
            { Stage.War, "WAR!" },
        };
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
        public static readonly IReadOnlyDictionary<string, Stage> StageFormatedNames = new Dictionary<string, Stage>
        {
            { "dam", Stage.Dam },
            { "facility", Stage.Facility },
            { "runway", Stage.Runway },
            { "surface1", Stage.Surface1 },
            { "bunker1", Stage.Bunker1 },
            { "silo", Stage.Silo },
            { "frigate", Stage.Frigate },
            { "surface2", Stage.Surface2 },
            { "bunker2", Stage.Bunker2 },
            { "statue", Stage.Statue },
            { "archives", Stage.Archives },
            { "streets", Stage.Streets },
            { "depot", Stage.Depot },
            { "train", Stage.Train },
            { "jungle", Stage.Jungle },
            { "control", Stage.Control },
            { "caverns", Stage.Caverns },
            { "cradle", Stage.Cradle },
            { "aztec", Stage.Aztec },
            { "egypt", Stage.Egypt },
            { "defection", Stage.Defection },
            { "investigation", Stage.Investigation },
            { "extraction", Stage.Extraction },
            { "villa", Stage.Villa },
            { "chicago", Stage.Chicago },
            { "g5", Stage.G5 },
            { "infiltration", Stage.Infiltration },
            { "rescue", Stage.Rescue },
            { "escape", Stage.Escape },
            { "airbase", Stage.AirBase },
            { "airforceone", Stage.AirForceOne },
            { "crashsite", Stage.CrashSite },
            { "pelagicii", Stage.PelagicII },
            { "deepsea", Stage.DeepSea },
            { "ci", Stage.CI },
            { "attackship", Stage.AttackShip },
            { "skedarruins", Stage.SkedarRuins },
            { "mbr", Stage.MBR },
            { "maiansos", Stage.MaianSOS },
            { "war!", Stage.War }
        };
        private static readonly Dictionary<(Level, Game), string> _levelLabels = new Dictionary<(Level, Game), string>
        {
            { (Level.Easy, Game.GoldenEye), "Agent" },
            { (Level.Medium, Game.GoldenEye), "Secret agent" },
            { (Level.Hard, Game.GoldenEye), "00 agent" },
            { (Level.Easy, Game.PerfectDark), "Agent" },
            { (Level.Medium, Game.PerfectDark), "Special agent" },
            { (Level.Hard, Game.PerfectDark), "Perfect agent" },
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

        public static Stage? GetStageFromLabel(string stageLabel)
        {
            var matches = _stageLabels.Where(_ =>
                _.Value.Equals(stageLabel, StringComparison.InvariantCultureIgnoreCase));

            return matches.Any() ? matches.First().Key : default(Stage?);
        }

        public static Level? GetLevelFromLabel(string levelLabel)
        {
            var matches = _levelLabels.Where(_ =>
                _.Value.Equals(levelLabel, StringComparison.InvariantCultureIgnoreCase));

            return matches.Any() ? matches.First().Key.Item1 : default(Level?);
        }

        public static string GetGameUrlName(this Game game)
        {
            return _eliteUrlName[game];
        }

        public static string GetLabel(this Level level, Game game)
        {
            return _levelLabels.ContainsKey((level, game)) ?
                _levelLabels[(level, game)] : DefaultLabel;
        }

        internal static List<T> WithRanks<T, TValue>(
            this List<T> rankings,
            Func<T, TValue> getComparedValue)
            where T : Ranking
            where TValue : IEquatable<TValue>
        {
            for (int i = 0; i < rankings.Count; i++)
            {
                rankings[i].SetRank(
                    i == 0 ? null : rankings[i - 1],
                    getComparedValue);
            }
            return rankings;
        }
    }
}
