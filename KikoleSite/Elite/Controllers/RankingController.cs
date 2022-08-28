using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;
using KikoleSite.Elite.Models;
using KikoleSite.Elite.Providers;
using KikoleSite.Elite.ViewDatas;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Elite.Controllers
{
    [Route("the-elite")]
    public class SimulatedRankingController : Controller
    {
        private const int MaxRankDisplay = 500;
        private const string StageImagePath = @"/images/elite/{0}.jpg";
        private const string RankingViewName = "SimulatedRanking";
        private const string PlayersViewName = "Players";
        private const string PlayerDetailsViewName = "PlayerDetails";

        private readonly IStatisticsProvider _statisticsProvider;

        public SimulatedRankingController(IStatisticsProvider statisticsProvider)
        {
            _statisticsProvider = statisticsProvider;
        }

        [HttpGet("players")]
        public async Task<IActionResult> GetPlayersAsync()
        {
            return await View(
                PlayersViewName,
                "Players list",
                async () =>
                {
                    var players = await _statisticsProvider.GetPlayersAsync().ConfigureAwait(false);

                    return players.Select(p => p.ToPlayerItemData()).ToList();
                }).ConfigureAwait(false);
        }

        [HttpGet("games/{game}/player-rankings")]
        public async Task<IActionResult> GetRankingByPlayerAsync(
            [FromRoute] Game game,
            [FromQuery] long playerId,
            [FromQuery] DateTime? rankingDate)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _)
                || playerId <= 0)
            {
                return BadRequest();
            }

            return await SimulateRankingInternalAsync(game, rankingDate, playerId)
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/time-frame-rankings")]
        public async Task<IActionResult> GetRankingByTimeFrameAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [Required][FromQuery] DateTime rankingStartDate)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _)
                || rankingStartDate > DateTime.Now
                || (rankingDate.HasValue && rankingDate < rankingStartDate))
            {
                return BadRequest();
            }

            return await SimulateRankingInternalAsync(game, rankingDate, rankingStartDate: rankingStartDate)
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/engine-rankings")]
        public async Task<IActionResult> GetRankingByEngineAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] int engine)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _))
            {
                return BadRequest();
            }

            if (!SystemExtensions.Enumerate<Engine>().Any(e => engine == (int)e))
            {
                return BadRequest();
            }

            return await SimulateRankingInternalAsync(game, rankingDate, engine: engine)
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/players/{playerId}/details")]
        public async Task<IActionResult> GetPlayerDetailsForSpecifiedRankingAsync(
            [FromRoute] Game game,
            [FromRoute] long playerId,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] DateTime? rankingStartDate,
            [FromQuery] int? engine)
        {
            return await View(
                PlayerDetailsViewName,
                $"PlayerID {playerId} - {game} times",
                async () =>
                {
                    var rankingEntries = await GetRankingsWithParamsAsync(game,
                        rankingDate ?? DateTime.Now, playerId, rankingStartDate, engine)
                    .ConfigureAwait(false);

                    var pRanking = rankingEntries.Single(r => r.Player.Id == playerId);

                    return pRanking.ToPlayerDetailsViewData(StageImagePath);
                }).ConfigureAwait(false);
        }

        private async Task<IActionResult> SimulateRankingInternalAsync(
            Game game,
            DateTime? rankingDate,
            long? playerId = null,
            DateTime? rankingStartDate = null,
            int? engine = null)
        {
            return await View(
                RankingViewName,
                "The GoldenEye/PerfectDark World Records and Rankings SIMULATOR",
                async () =>
                {
                    var rankingEntries = await GetRankingsWithParamsAsync(game, rankingDate ?? DateTime.Now, playerId, rankingStartDate, engine).ConfigureAwait(false);

                    var pointsRankingEntries = rankingEntries
                        .Where(r => r.Rank <= MaxRankDisplay)
                        .Select(r => r.ToPointsRankingItemData())
                        .ToList();

                    // this does not manage equality between two global times
                    // ie one player will be ranked above/below the other one
                    int rank = 1;
                    var timeRankingEntries = rankingEntries
                        .OrderBy(x => x.CumuledTime)
                        .Take(MaxRankDisplay)
                        .Select(r => r.ToTimeRankingItemData(rank++))
                        .ToList();

                    var secondsLevel = SystemExtensions.Enumerate<Level>().ToDictionary(l => l, l => 0);
                    var stageWorldRecordEntries = game.GetStages()
                        .Select(s => s.ToStageWorldRecordItemData(rankingEntries, secondsLevel, StageImagePath))
                        .ToList();

                    return new SimulatedRankingViewData
                    {
                        CombinedTime = new TimeSpan(0, 0, secondsLevel.Values.Sum()),
                        EasyCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Easy]),
                        MediumCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Medium]),
                        HardCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Hard]),
                        PointsRankingEntries = pointsRankingEntries,
                        TimeRankingEntries = timeRankingEntries,
                        StageWorldRecordEntries = stageWorldRecordEntries,
                        EasyLabel = Level.Easy.GetLabel(game),
                        MediumLabel = Level.Medium.GetLabel(game),
                        HardLabel = Level.Hard.GetLabel(game),
                        EasyShortLabel = Level.Easy.GetLabel(game, true),
                        MediumShortLabel = Level.Medium.GetLabel(game, true),
                        HardShortLabel = Level.Hard.GetLabel(game, true)
                    };
                }).ConfigureAwait(false);
        }

        private async Task<List<RankingEntry>> GetRankingsWithParamsAsync(
            Game game,
            DateTime rankingDate,
            long? playerId,
            DateTime? rankingStartDate,
            int? engine)
        {
            var request = new RankingRequest
            {
                Game = game,
                FullDetails = true,
                RankingDate = rankingDate,
                Engine = (Engine?)engine,
                IncludeUnknownEngine = true,
                RankingStartDate = rankingStartDate
            };

            if (playerId.HasValue)
            {
                request.PlayerVsLegacy = (playerId.Value, rankingDate);
                request.RankingDate = DateTime.Now;
            }

            var rankingEntriesBase = await _statisticsProvider
                .GetRankingEntriesAsync(request)
                .ConfigureAwait(false);

            return rankingEntriesBase.Select(r => r as RankingEntry).ToList();
        }

        private async Task<IActionResult> View(
            string viewName,
            string title,
            Func<Task<object>> getDatasFunc)
        {
            return View($"Elite/Views/Template.cshtml", new BaseViewData
            {
                Data = await getDatasFunc().ConfigureAwait(false),
                Name = $"~/Elite/Views/{viewName}.cshtml",
                Title = title
            });
        }
    }
}
