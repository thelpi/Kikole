using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Providers;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Elite.Controllers
{
    public class EliteController : Controller
    {
        private readonly IStatisticsProvider _statisticsProvider;

        public EliteController(IStatisticsProvider statisticsProvider)
        {
            _statisticsProvider = statisticsProvider;
        }

        [HttpGet("the-elite/games/{game}/longest-standings")]
        public async Task<JsonResult> GetLongestStandingsAsync(
            [FromRoute] Game game,
            [FromQuery][Required] StandingType standingType,
            [FromQuery] bool? stillOngoing,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? count,
            [FromQuery] Engine? engine)
        {
            var standings = await _statisticsProvider
                .GetLongestStandingsAsync(game, endDate, standingType, stillOngoing, engine)
                .ConfigureAwait(false);

            return Json(standings.Take(count ?? 100).ToList());
        }

        [HttpGet("the-elite/stages/{stage}/leaderboard-history")]
        public async Task<JsonResult> GetStageLeaderboardHistoryAsync(
            [FromRoute] Stage stage,
            [FromQuery] LeaderboardGroupOptions groupOption,
            [FromQuery] int daysStep)
        {
            var datas = await _statisticsProvider
                .GetStageLeaderboardHistoryAsync(stage, groupOption, daysStep)
                .ConfigureAwait(false);

            return Json(datas);
        }

        [HttpGet("the-elite/players")]
        public async Task<JsonResult> GetPlayersAsync()
        {
            var players = await _statisticsProvider
                .GetPlayersAsync()
                .ConfigureAwait(false);

            return Json(players);
        }
    }
}
