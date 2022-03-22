using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class LeaderboardController : KikoleBaseController
    {
        public LeaderboardController(IApiProvider apiProvider)
            : base(apiProvider)
        { }
        
        public async Task<IActionResult> Index([FromQuery] ulong userId)
        {
            if (userId == 0)
            {
                return await Index().ConfigureAwait(false);
            }

            var stats = await _apiProvider
                .GetUserStatsAsync(userId)
                .ConfigureAwait(false);

            if (stats == null)
            {
                return await Index().ConfigureAwait(false);
            }

            var badges = await _apiProvider
                .GetUserBadgesAsync(userId)
                .ConfigureAwait(false);

            var allBadges = await _apiProvider
                .GetBadgesAsync()
                .ConfigureAwait(false);

            var knownAnswers = new List<string>();
            var (token, _) = this.GetAuthenticationCookie();
            if (!string.IsNullOrWhiteSpace(token))
            {
                knownAnswers = (await _apiProvider
                    .GetUserKnownPlayersAsync(token)
                    .ConfigureAwait(false))
                    .ToList();
            }

            return View("User", new UserStatsModel(stats, badges, allBadges, knownAnswers));
        }

        private async Task<IActionResult> Index()
        {
            var leaders = await _apiProvider
                .GetLeadersAsync(LeaderSort.TotalPoints, null)
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
                    dtNull)
                .ConfigureAwait(false);

            model.TodayLeaders = await _apiProvider
                .GetTodayLeadersAsync()
                .ConfigureAwait(false);

            return View(model);
        }
    }
}
