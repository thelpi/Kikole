using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Models;

namespace KikoleSite.Elite.Providers
{
    public interface IStatisticsProvider
    {
        Task<IReadOnlyCollection<Player>> GetPlayersAsync();

        Task<IReadOnlyCollection<Standing>> GetLongestStandingsAsync(
            Game game,
            DateTime? endDate,
            StandingType standingType,
            bool? stillOngoing,
            Engine? engine);

        Task<IReadOnlyCollection<StageLeaderboard>> GetStageLeaderboardHistoryAsync(
            Stage stage,
            LeaderboardGroupOptions groupOption,
            int daysStep);

        Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(
            RankingRequest request);
    }
}
