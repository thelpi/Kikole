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

        private static readonly object _lock = new object();
        private static Task _runningTask;
        private static object _taskResult;
        private static string _taskName;

        public EliteController(IStatisticsProvider statisticsProvider,
            IIntegrationProvider integrationProvider)
        {
            _statisticsProvider = statisticsProvider;
            _integrationProvider = integrationProvider;
        }

        [HttpGet("players/refresh")]
        public JsonResult RefreshPlayers([FromQuery] bool addTimesForNewPlayers)
        {
            var json = StartAsyncTaskAndGetJson(() =>
                _integrationProvider.RefreshPlayersAsync(addTimesForNewPlayers),
                nameof(RefreshPlayers));
            return Json(json);
        }

        [HttpGet("entries/{game}/refresh")]
        public JsonResult RefreshGameEntries([FromRoute] Game game)
        {
            var json = StartAsyncTaskAndGetJson(() =>
                _integrationProvider.RefreshAllEntriesAsync(game),
                nameof(RefreshGameEntries));
            return Json(json);
        }

        [HttpGet("entries/refresh")]
        public JsonResult RefreshRecentEntries([Required][FromQuery] DateTime fromDate)
        {
            var json = StartAsyncTaskAndGetJson(() =>
                _integrationProvider.RefreshEntriesToDateAsync(fromDate),
                nameof(RefreshRecentEntries));
            return Json(json);
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

        private static object StartAsyncTaskAndGetJson<T>(Func<Task<T>> asyncCall, string taskName)
        {
            object returned = null;
            lock (_lock)
            {
                if (_runningTask != null)
                {
                    if (!_runningTask.IsCompleted)
                    {
                        returned = new { msg = $"Task \"{_taskName}\" is already running." };
                    }
                    else
                    {
                        returned = new { msg = $"Task \"{_taskName}\" just finished.", result = _taskResult };
                        _runningTask = null;
                        _taskName = null;
                    }
                }
                else
                {
                    _taskName = taskName;
                    _runningTask = Task.Run(async () =>
                    {
                        try
                        {
                            _taskResult = await asyncCall().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _taskResult = new { globalTaskError = $"{ex.Message}\n{ex.StackTrace}" };
                        }
                    });
                    returned = new { msg = $"Task \"{_taskName}\" has been launched." };
                }
            }
            return returned;
        }
    }
}
