using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Enums;
using KikoleSite.Models;

namespace KikoleSite.Providers
{
    public interface IStatisticsProvider
    {
        Task<IReadOnlyCollection<Player>> GetPlayersAsync(
            bool useCache = false,
            string pattern = null);

        Task<IReadOnlyCollection<Standing>> GetLongestStandingsAsync(
            Game game,
            DateTime? endDate,
            StandingType standingType,
            bool? stillOngoing,
            Engine? engine,
            uint? playerId,
            uint? slayerPlayerId);

        Task<IReadOnlyCollection<StageLeaderboard>> GetStageLeaderboardHistoryAsync(
            Stage stage,
            LeaderboardGroupOptions groupOption,
            int daysStep,
            uint? playerId);

        Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(
            RankingRequest request);

        Task<PlayerRankingHistory> GetPlayerRankingHistoryAsync(
            Game game,
            uint playerId);

        Task<(DateTime? firstDate, DateTime? lastDate)> GetPlayerActivityDatesAsync(
            Game game,
            uint playerId);

        Task<IReadOnlyCollection<SweepLight>> GetSweepsAsync(
            Game game,
            uint? playerId,
            bool untied,
            Engine? engine,
            bool? stillOngoing);

        Task<IReadOnlyCollection<RelativeEntry>> GetRelativeDifficultyEntriesAsync(
            Game game,
            DateTime date,
            bool withoutCurrentUntieds);

        Task<IReadOnlyCollection<LatestPoint>> GetLatestPointsAsync(
            Game game,
            int minimalPoints,
            bool discardEntryWhenBetter);
    }
}
