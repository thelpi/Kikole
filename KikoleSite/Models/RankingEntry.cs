using System.Collections.Generic;
using System.Linq;
using KikoleSite.Dtos;
using KikoleSite.Enums;
using KikoleSite.Extensions;

namespace KikoleSite.Models
{
    public class RankingEntry : RankingEntryLight
    {
        private readonly Dictionary<Level, int> _levelPoints;
        private readonly Dictionary<Level, int> _levelUntiedRecordsCount;
        private readonly Dictionary<Level, int> _levelRecordsCount;
        private readonly Dictionary<Level, int> _levelCumuledTime;
        private readonly Dictionary<Stage, Dictionary<Level, (int, int, int?, System.DateTime?)>> _details;

        public IReadOnlyDictionary<Level, int> LevelPoints => _levelPoints;
        public IReadOnlyDictionary<Level, int> LevelUntiedRecordsCount => _levelUntiedRecordsCount;
        public IReadOnlyDictionary<Level, int> LevelRecordsCount => _levelRecordsCount;
        public IReadOnlyDictionary<Level, int> LevelCumuledTime => _levelCumuledTime;
        public IReadOnlyDictionary<Stage, IReadOnlyDictionary<Level, (int, int, int?, System.DateTime?)>> Details => _details.ToDictionary(d => d.Key, d => (IReadOnlyDictionary<Level, (int, int, int?, System.DateTime?)>)d.Value);

        internal RankingEntry(Game game, PlayerDto player) : base(game, player)
        {
            _details = new Dictionary<Stage, Dictionary<Level, (int, int, int?, System.DateTime?)>>();

            _levelPoints = ToLevelDictionary(0);
            _levelUntiedRecordsCount = ToLevelDictionary(0);
            _levelRecordsCount = ToLevelDictionary(0);

            _levelCumuledTime = ToLevelDictionary(UnsetTimeValueSeconds * Game.GetStages().Count);
        }

        internal override void AddStageAndLevelData(RankingDto ranking, RankingEntryDto rankingEntry, bool untied)
        {
            base.AddStageAndLevelData(ranking, rankingEntry, untied);

            if (rankingEntry.Rank == 1)
            {
                _levelRecordsCount[ranking.Level]++;
                if (untied)
                {
                    _levelUntiedRecordsCount[ranking.Level]++;
                }
            }

            _levelPoints[ranking.Level] += rankingEntry.Points;

            GetDetailsByLevel(ranking.Stage).Add(ranking.Level, (rankingEntry.Rank, rankingEntry.Points, rankingEntry.Time, rankingEntry.EntryDate));

            if (rankingEntry.Time < UnsetTimeValueSeconds)
            {
                _levelCumuledTime[ranking.Level] -= UnsetTimeValueSeconds - rankingEntry.Time;
            }
        }

        private Dictionary<Level, (int, int, int?, System.DateTime?)> GetDetailsByLevel(Stage stage)
        {
            Dictionary<Level, (int, int, int?, System.DateTime?)> detailsByLevel;
            if (!_details.ContainsKey(stage))
            {
                detailsByLevel = new Dictionary<Level, (int, int, int?, System.DateTime?)>();
                _details.Add(stage, detailsByLevel);
            }
            else
            {
                detailsByLevel = _details[stage];
            }

            return detailsByLevel;
        }

        private static Dictionary<Level, T> ToLevelDictionary<T>(T value)
        {
            return SystemExtensions.Enumerate<Level>().ToDictionary(l => l, l => value);
        }
    }
}
