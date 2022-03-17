using System;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class LeaderboardController : KikoleBaseController
    {
        const int limit = 50;

        private readonly IApiProvider _apiProvider;

        public LeaderboardController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }
        
        public async Task<IActionResult> Index([FromQuery] ulong userId)
        {
            if (userId == 0)
            {
                return await Index().ConfigureAwait(false);
            }

            var stats = await _apiProvider
                .GetUserStats(userId)
                .ConfigureAwait(false);

            if (stats == null)
            {
                return await Index().ConfigureAwait(false);
            }

            var badges = await _apiProvider
                .GetUserBadges(userId)
                .ConfigureAwait(false);

            return View("User", new UserStatsModel(stats, badges));
        }

        private async Task<IActionResult> Index()
        {
            var leaders = await _apiProvider
                .GetLeadersAsync(LeaderSort.TotalPoints, limit, null)
                .ConfigureAwait(false);

            return View(new LeaderboardModel
            {
                MinimalDate = null,
                Leaders = leaders,
                SortType = LeaderSort.TotalPoints,
                TodayLeaders = await _apiProvider
                    .GetTodayLeadersAsync()
                    .ConfigureAwait(false)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Index(LeaderboardModel model)
        {
            DateTime? dtNull = null;
            if (DateTime.TryParse(model.MinimalDate, out var dt))
                dtNull = dt;

            model.MinimalDate = dtNull?.ToString("yyyy-MM-dd");

            model.Leaders = await _apiProvider
                .GetLeadersAsync(
                    model.SortType,
                    limit,
                    dtNull)
                .ConfigureAwait(false);

            model.TodayLeaders = await _apiProvider
                .GetTodayLeadersAsync()
                .ConfigureAwait(false);

            return View(model);
        }
    }
}
