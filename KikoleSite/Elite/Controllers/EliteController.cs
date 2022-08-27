using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Providers;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Elite.Controllers
{
    [Route("the-elite")]
    public class EliteController : Controller
    {
        private readonly IStatisticsProvider _statisticsProvider;
        private readonly IIntegrationProvider _integrationProvider;

        public EliteController(IStatisticsProvider statisticsProvider,
            IIntegrationProvider integrationProvider)
        {
            _statisticsProvider = statisticsProvider;
            _integrationProvider = integrationProvider;
        }

        [HttpGet("players/refresh")]
        public async Task<JsonResult> RefreshPlayersAsync([FromQuery] bool addTimesForNewPlayers)
        {
            var refreshResult = await _integrationProvider
                .RefreshPlayersAsync(addTimesForNewPlayers)
                .ConfigureAwait(false);

            return Json(refreshResult);
        }

        [HttpGet("entries/{game}/refresh")]
        public async Task<JsonResult> RefreshEntriesAsync([FromRoute] Game game)
        {
            var refreshResult = await _integrationProvider
                .RefreshAllEntriesAsync(game)
                .ConfigureAwait(false);

            return Json(refreshResult);
        }

        [HttpGet("entries/refresh")]
        public async Task<JsonResult> RefreshEntriesAsync([Required][FromQuery] DateTime fromDate)
        {
            var refreshResult = await _integrationProvider
                .RefreshEntriesToDateAsync(fromDate)
                .ConfigureAwait(false);

            return Json(refreshResult);
        }

        [HttpGet("games/{game}/longest-standings")]
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

        [HttpGet("stages/{stage}/leaderboard-history")]
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

        [HttpGet("players")]
        public async Task<JsonResult> GetPlayersAsync()
        {
            var players = await _statisticsProvider
                .GetPlayersAsync()
                .ConfigureAwait(false);

            return Json(players);
        }
    }
}
