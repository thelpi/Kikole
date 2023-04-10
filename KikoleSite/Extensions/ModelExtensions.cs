using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Enums;
using KikoleSite.Models;

namespace KikoleSite.Extensions
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
        private static readonly IReadOnlyDictionary<string, ControlStyles> _controlStyleConverters = new Dictionary<string, ControlStyles>
        {
            { "1.1", ControlStyles.OnePointOne },
            { "1.2", ControlStyles.OnePointTwo },
            { "1.3", ControlStyles.OnePointThree },
            { "1.4", ControlStyles.OnePointFour },
            { "2.1", ControlStyles.TwoPointOne },
            { "2.2", ControlStyles.TwoPointTwo },
            { "2.3", ControlStyles.TwoPointThree },
            { "2.4", ControlStyles.TwoPointFour }
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
        private static readonly Dictionary<(Level, Game), string> _levelShortLabels = new Dictionary<(Level, Game), string>
        {
            { (Level.Easy, Game.GoldenEye), "A" },
            { (Level.Medium, Game.GoldenEye), "S" },
            { (Level.Hard, Game.GoldenEye), "00A" },
            { (Level.Easy, Game.PerfectDark), "A" },
            { (Level.Medium, Game.PerfectDark), "SA" },
            { (Level.Hard, Game.PerfectDark), "PA" },
        };

        public static ControlStyles? ToControlStyle(string controlStyleLabel)
        {
            return controlStyleLabel != null && _controlStyleConverters.ContainsKey(controlStyleLabel) ?
                _controlStyleConverters[controlStyleLabel] : default(ControlStyles?);
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

        public static string GetLabel(this Level level, Game game, bool shortVersion = false)
        {
            return shortVersion
                ? _levelShortLabels[(level, game)]
                : _levelLabels[(level, game)];
        }

        internal static List<T> WithRanks<T, TValue>(
            this List<T> rankings,
            Func<T, TValue> getComparedValue)
            where T : Ranking
            where TValue : IEquatable<TValue>
        {
            for (var i = 0; i < rankings.Count; i++)
            {
                rankings[i].SetRank(
                    i == 0 ? null : rankings[i - 1],
                    getComparedValue);
            }
            return rankings;
        }

        internal static void ManageDateLessEntries(
            this List<Dtos.EntryDto> entries,
            NoDateEntryRankingRule rule,
            DateTime now)
        {
            if (rule == NoDateEntryRankingRule.Ignore)
            {
                entries.RemoveAll(e => !e.Date.HasValue);
                return;
            }

            var game = entries.First().Stage.GetGame();

            var dateMinMaxPlayer = new Dictionary<long, (DateTime Min, DateTime Max, IReadOnlyCollection<Dtos.EntryDto> Entries)>();

            var dateLessEntries = entries.Where(e => !e.Date.HasValue).ToList();
            foreach (var entry in dateLessEntries)
            {
                if (!dateMinMaxPlayer.ContainsKey(entry.PlayerId))
                {
                    var dateMin = entries
                        .Where(e => e.PlayerId == entry.PlayerId && e.Date.HasValue)
                        .Select(e => e.Date.Value)
                        .Concat(game.GetEliteFirstDate().Yield())
                        .Min();
                    var dateMax = entries.Where(e => e.PlayerId == entry.PlayerId).Max(e => e.Date ?? Player.LastEmptyDate);
                    dateMinMaxPlayer.Add(entry.PlayerId, (dateMin, dateMax, entries.Where(e => e.PlayerId == entry.PlayerId).ToList()));
                }

                // Same time with a known date (possible for another engine/system)
                var sameEntry = dateMinMaxPlayer[entry.PlayerId].Entries.FirstOrDefault(e => e.Stage == entry.Stage && e.Level == entry.Level && e.Time == entry.Time && e.Date.HasValue);
                // Better time (closest to current) with a known date
                var betterEntry = dateMinMaxPlayer[entry.PlayerId].Entries.OrderBy(e => e.Time).FirstOrDefault(e => e.Stage == entry.Stage && e.Level == entry.Level && e.Time < entry.Time && e.Date.HasValue);
                // Worse time (closest to current) with a known date
                var worseEntry = dateMinMaxPlayer[entry.PlayerId].Entries.OrderByDescending(e => e.Time).FirstOrDefault(e => e.Stage == entry.Stage && e.Level == entry.Level && e.Time < entry.Time && e.Date.HasValue);

                if (sameEntry != null)
                {
                    // use the another engine/system date as the current date
                    entry.Date = sameEntry.Date;
                    entry.IsSimulatedDate = true;
                }
                else
                {
                    var realMin = dateMinMaxPlayer[entry.PlayerId].Min;
                    if (worseEntry != null && worseEntry.Date > realMin)
                    {
                        realMin = worseEntry.Date.Value;
                    }

                    var realMax = dateMinMaxPlayer[entry.PlayerId].Max;
                    if (betterEntry != null && betterEntry.Date < realMax)
                    {
                        realMax = betterEntry.Date.Value;
                    }

                    // when the min / max theoric is too wide to set a proper date
                    if (realMin == game.GetEliteFirstDate() && realMax == Player.LastEmptyDate)
                    {
                        entries.Remove(entry);
                        continue;
                    }

                    switch (rule)
                    {
                        case NoDateEntryRankingRule.Average:
                            entry.Date = realMin.AddDays((realMax - realMin).TotalDays / 2).Date;
                            entry.IsSimulatedDate = true;
                            break;
                        case NoDateEntryRankingRule.Max:
                            entry.Date = realMax;
                            entry.IsSimulatedDate = true;
                            break;
                        case NoDateEntryRankingRule.Min:
                            entry.Date = realMin;
                            entry.IsSimulatedDate = true;
                            break;
                        case NoDateEntryRankingRule.PlayerHabit:
                            var entriesBetween = dateMinMaxPlayer[entry.PlayerId].Entries
                                .Where(e => e.Date < realMax && e.Date > realMin)
                                .Select(e => Convert.ToInt32((now - e.Date.Value).TotalDays))
                                .ToList();
                            if (entriesBetween.Count == 0)
                            {
                                entry.Date = realMin.AddDays((realMax - realMin).TotalDays / 2).Date;
                            }
                            else
                            {
                                var avgDays = entriesBetween.Average();
                                entry.Date = now.AddDays(-avgDays).Date;
                            }
                            entry.IsSimulatedDate = true;
                            break;
                    }
                }
            }
        }
    }
}
