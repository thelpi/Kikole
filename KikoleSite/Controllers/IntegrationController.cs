using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using KikoleSite.Enums;
using KikoleSite.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Controllers
{
    public class IntegrationController : Controller
    {
        private readonly IIntegrationProvider _integrationProvider;
        private readonly string _secret;

        private static readonly object _lock = new object();
        private static Task _runningTask;
        private static object _taskResult;
        private static string _taskName;

        public IntegrationController(IConfiguration configuration, IIntegrationProvider integrationProvider)
        {
            _integrationProvider = integrationProvider;
            _secret = configuration.GetValue<string>("TheEliteIntegrationSecret");
        }

        [HttpGet("rankings/{game}/refresh")]
        public JsonResult RefreshRankings([FromRoute] Game game, [FromQuery] DateTime? startDate, [FromQuery] Stage? stage, [FromQuery] Level? level, [FromQuery] string secret)
        {
            var json = StartAsyncTaskAndGetJson(() =>
                _integrationProvider.ComputeRankingsAsync(game, startDate, stage, level),
                nameof(RefreshPlayers),
                secret);
            return Json(json);
        }

        [HttpGet("players/refresh")]
        public JsonResult RefreshPlayers(
            [FromQuery] bool addTimesForNewPlayers,
            [FromQuery] bool refreshExistingPlayers,
            [FromQuery] string secret)
        {
            var json = StartAsyncTaskAndGetJson(() =>
                _integrationProvider.RefreshPlayersAsync(addTimesForNewPlayers, refreshExistingPlayers),
                nameof(RefreshPlayers),
                secret);
            return Json(json);
        }

        [HttpGet("entries/{game}/refresh")]
        public JsonResult RefreshGameEntries([FromRoute] Game game, [FromQuery] string secret)
        {
            var json = StartAsyncTaskAndGetJson(() =>
                _integrationProvider.RefreshAllEntriesAsync(game),
                nameof(RefreshGameEntries),
                secret);
            return Json(json);
        }

        [HttpGet("entries/refresh")]
        public JsonResult RefreshRecentEntries([Required][FromQuery] DateTime fromDate, [FromQuery] string secret)
        {
            var json = StartAsyncTaskAndGetJson(() =>
                _integrationProvider.RefreshEntriesToDateAsync(fromDate),
                nameof(RefreshRecentEntries),
                secret);
            return Json(json);
        }

        [HttpGet("players/{playerId}/entries/refresh")]
        public JsonResult RefreshPlayerEntries([FromRoute] uint playerId, [FromQuery] string secret)
        {
            var json = StartAsyncTaskAndGetJson(() =>
                _integrationProvider.RefreshPlayerEntriesAsync(playerId),
                nameof(RefreshPlayerEntries),
                secret);
            return Json(json);
        }

        [HttpGet("current-action")]
        public JsonResult GetCurrentAction([FromQuery] string secret)
        {
            var json = StartAsyncTaskAndGetJson<object>(null, null, secret);
            return Json(json);
        }

        private object StartAsyncTaskAndGetJson<T>(Func<Task<T>> asyncCall, string taskName, string secret)
        {
            if (!_secret.Equals(secret, StringComparison.Ordinal))
            {
                return new { msg = "You're not allowed to do this action." };
            }

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
                    if (asyncCall != null)
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
                    else
                    {
                        returned = new { msg = "Nothing is currently running." };
                    }
                }
            }
            return returned;
        }
    }
}
