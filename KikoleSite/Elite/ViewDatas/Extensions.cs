using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;
using KikoleSite.Elite.Models;

namespace KikoleSite.Elite.ViewDatas
{
    internal static class Extensions
    {
        internal static StageWorldRecordItemData ToStageWorldRecordItemData(this Stage stage,
            List<RankingEntry> rankingEntries,
            Dictionary<Level, int> secondsLevelCollector,
            string stageImagePath)
        {
            var invalid = false;
            var coloredInitialsLevel = new Dictionary<Level, List<(string, string, string)>>();
            var secondsLevelStage = new Dictionary<Level, int>();
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var bestTime = GetStageAndLevelBestTime(rankingEntries, stage, level);
                if (!bestTime.HasValue)
                {
                    invalid = true;
                    break;
                }
                secondsLevelStage.Add(level, bestTime.Value);
                coloredInitialsLevel.Add(level, GetPlayersRankedAtStageAndLevelTime(rankingEntries, stage, level, bestTime.Value));
                secondsLevelCollector[level] += bestTime.Value;
            }

            // sets the whole stage as invalid...
            if (invalid) return null;

            return new StageWorldRecordItemData
            {
                EasyColoredInitials = coloredInitialsLevel[Level.Easy],
                EasyTime = new TimeSpan(0, 0, secondsLevelStage[Level.Easy]),
                MediumColoredInitials = coloredInitialsLevel[Level.Medium],
                MediumTime = new TimeSpan(0, 0, secondsLevelStage[Level.Medium]),
                HardColoredInitials = coloredInitialsLevel[Level.Hard],
                HardTime = new TimeSpan(0, 0, secondsLevelStage[Level.Hard]),
                Image = string.Format(stageImagePath, (int)stage),
                Name = stage.ToString(),
                Code = $"s{(int)stage}"
            };
        }

        internal static PlayerDetailsViewData ToPlayerDetailsViewData(this RankingEntry entry, string imagePath)
        {
            var localDetails = new Dictionary<Stage, IReadOnlyDictionary<Level, (int, int, long?, DateTime?)>>();
            foreach (var stage in entry.Game.GetStages())
                localDetails.Add(stage, entry.Details.ContainsKey(stage) ? entry.Details[stage] : null);

            return new PlayerDetailsViewData
            {
                DetailsByStage = localDetails.Keys.Select(sk => localDetails[sk].ToPlayerStageDetailsItemData(sk, imagePath)).ToList(),
                EasyPoints = entry.LevelPoints[Level.Easy],
                EasyTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Easy]),
                Game = entry.Game,
                HardPoints = entry.LevelPoints[Level.Hard],
                HardTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Hard]),
                MediumPoints = entry.LevelPoints[Level.Medium],
                MediumTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Medium]),
                OverallPoints = entry.Points,
                OverallRanking = entry.Rank,
                OverallTime = new TimeSpan(0, 0, (int)entry.CumuledTime),
                PlayerName = entry.PlayerName
            };
        }

        internal static TimeRankingItemData ToTimeRankingItemData(this RankingEntry entry, int rank)
        {
            return new TimeRankingItemData
            {
                EasyTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Easy]),
                HardTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Hard]),
                MediumTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Medium]),
                PlayerColor = entry.Player.Color,
                PlayerName = entry.PlayerName,
                Rank = rank,
                TotalTime = new TimeSpan(0, 0, (int)entry.CumuledTime)
            };
        }

        internal static PointsRankingItemData ToPointsRankingItemData(this RankingEntry entry)
        {
            return new PointsRankingItemData
            {
                EasyPoints = entry.LevelPoints[Level.Easy],
                HardPoints = entry.LevelPoints[Level.Hard],
                MediumPoints = entry.LevelPoints[Level.Medium],
                PlayerColor = entry.Player.Color,
                PlayerName = entry.PlayerName,
                Rank = entry.Rank,
                TotalPoints = entry.Points
            };
        }

        internal static PlayerItemData ToPlayerItemData(this Player player)
        {
            return new PlayerItemData
            {
                Color = player.Color,
                Id = player.Id,
                RealName = player.RealName,
                SurName = player.SurName
            };
        }

        internal static bool IsFullStage(this ChronologyTypeItemData chronologyType)
        {
            return chronologyType == ChronologyTypeItemData.Ranking
                || chronologyType == ChronologyTypeItemData.Leaderboard;
        }

        internal static LeaderboardGroupOptions ToLeaderboardGroupOption(this ChronologyTypeItemData chronologyType)
        {
            return chronologyType switch
            {
                ChronologyTypeItemData.Leaderboard => LeaderboardGroupOptions.RankedTop10,
                _ => LeaderboardGroupOptions.None,
            };
        }

        internal static Func<StageLeaderboardItem, double> GetOpacityFunc(this ChronologyTypeItemData chronologyType)
        {
            return chronologyType switch
            {
                ChronologyTypeItemData.Leaderboard =>
                    it => it.Rank > 10 ? 0 : (11 - it.Rank) / (double)10,
                _ =>
                    it => (0.00083 * Math.Pow(it.Points, 2) + 0.0839) / 100,
            };
        }

        internal static StandingType? ToStandingType(this ChronologyTypeItemData chronologyType)
        {
            return chronologyType switch
            {
                ChronologyTypeItemData.AllUnslay => StandingType.BetweenTwoTimes,
                ChronologyTypeItemData.FirstUnslay => StandingType.FirstUnslayed,
                ChronologyTypeItemData.Untied => StandingType.UntiedExceptSelf,
                _ => null,
            };
        }

        internal static ChronologyCanvasItemData ToChronologyCanvasItemData(this Standing standing, bool anonymise, string anonymiseColorRgb)
        {
            return new ChronologyCanvasItemData
            {
                Stage = (int)standing.Stage,
                Level = (int)standing.Level,
                Color = anonymise
                    ? anonymiseColorRgb
                    : standing.Author.Color,
                Opacity = 1,
                Label = standing.ToString(),
                DaysBefore = standing.DaysBefore,
                Days = standing.Days.Value
            };
        }

        internal static ChronologyCanvasItemData ToChronologyCanvasItemData(this StageLeaderboardItem item, StageLeaderboard ld, ChronologyTypeItemData chronologyType, bool anonymise, string anonymiseColorRgb)
        {
            return new ChronologyCanvasItemData
            {
                Days = ld.Days,
                DaysBefore = ld.TotalDays,
                Color = anonymise
                    ? anonymiseColorRgb
                    : item.Player.Color,
                Label = $"Date:{ld.DateStart}\nPoints:{item.Points}\nRank:{item.Rank}",
                Opacity = chronologyType.GetOpacityFunc()(item),
                Stage = (int)ld.Stage
            };
        }

        private static List<(string, string, string)> GetPlayersRankedAtStageAndLevelTime(List<RankingEntry> rankingEntries, Stage stage, Level level, int bestTime)
        {
            return rankingEntries
                .Where(x => x.IsValid(stage, level)
                    && x.Details[stage][level].Item3 == bestTime)
                .OrderBy(x => x.Details[stage][level].Item4)
                .Select(x => (x.PlayerName.ToInitials(), PlayerColor: x.Player.Color, x.PlayerName))
                .ToList();
        }

        private static bool IsValid(this RankingEntry x, Stage stage, Level level)
        {
            return x.Details != null
                && x.Details.ContainsKey(stage)
                && x.Details[stage] != null
                && x.Details[stage].ContainsKey(level);
        }

        private static string ToInitials(this string playerName)
        {
            if (playerName == null)
                return string.Empty;

            var parts = playerName
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(_ => _.Trim())
                .ToArray();

            if (parts.Length == 0)
                return string.Empty;

            var initials = parts.Length < 2
                ? parts[0].Substring(0, 2)
                : string.Concat(parts[0].Substring(0, 1), parts[parts.Length == 2 ? 1 : 2].Substring(0, 1));

            return initials.ToUpperInvariant();
        }

        private static int? GetStageAndLevelBestTime(List<RankingEntry> rankingEntries, Stage stage, Level level)
        {
            var item = rankingEntries
                .Where(x => x.IsValid(stage, level)
                    && x.Details[stage][level].Item3.HasValue)
                .OrderBy(x => x.Details[stage][level].Item3)
                .FirstOrDefault();

            if (item == null)
            {
                return null;
            }

            return (int)item
                .Details[stage][level]
                .Item3;
        }

        private static PlayerStageDetailsItemData ToPlayerStageDetailsItemData(
            this IReadOnlyDictionary<Level, (int, int, long?, DateTime?)> lt,
            Stage s,
            string imagePath)
        {
            var d = new PlayerStageDetailsItemData
            {
                Stage = s,
                Image = string.Format(imagePath, (int)s)
            };

            if (lt != null)
            {
                if (lt.ContainsKey(Level.Easy) && lt[Level.Easy].Item3.HasValue)
                {
                    d.EasyPoints = lt[Level.Easy].Item2;
                    d.EasyTime = new TimeSpan(0, 0, (int)lt[Level.Easy].Item3);
                }
                if (lt.ContainsKey(Level.Medium) && lt[Level.Medium].Item3.HasValue)
                {
                    d.MediumPoints = lt[Level.Medium].Item2;
                    d.MediumTime = new TimeSpan(0, 0, (int)lt[Level.Medium].Item3);
                }
                if (lt.ContainsKey(Level.Hard) && lt[Level.Hard].Item3.HasValue)
                {
                    d.HardPoints = lt[Level.Hard].Item2;
                    d.HardTime = new TimeSpan(0, 0, (int)lt[Level.Hard].Item3);
                }
            }

            return d;
        }
    }
}
