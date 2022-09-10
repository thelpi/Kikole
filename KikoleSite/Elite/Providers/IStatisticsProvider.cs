using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Models;

namespace KikoleSite.Elite.Providers
{
    public interface IStatisticsProvider
    {
        Task<IReadOnlyCollection<Player>> GetPlayersAsync(bool useCache = false, string pattern = null);

        Task<IReadOnlyCollection<Standing>> GetLongestStandingsAsync(
            Game game,
            DateTime? endDate,
            StandingType standingType,
            bool? stillOngoing,
            Engine? engine,
            long? playerId,
            long? slayerPlayerId);

        Task<IReadOnlyCollection<StageLeaderboard>> GetStageLeaderboardHistoryAsync(
            Stage stage,
            LeaderboardGroupOptions groupOption,
            int daysStep,
            long? playerId);

        Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(
            RankingRequest request);

        Task<PlayerRankingHistory> GetPlayerRankingHistoryAsync(
            Game game,
            long playerId);

        Task<(DateTime? firstDate, DateTime? lastDate)> GetPlayerActivityDatesAsync(
            Game game,
            long playerId);
    }
}
