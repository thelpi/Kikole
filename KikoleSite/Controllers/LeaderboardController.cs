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
        
        [HttpGet]
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
            var (token, _) = GetAuthenticationCookie();
            if (!string.IsNullOrWhiteSpace(token))
            {
                knownAnswers = (await _apiProvider
                    .GetUserKnownPlayersAsync(token)
                    .ConfigureAwait(false))
                    .ToList();
            }

            return View("User", new UserStatsModel(stats, badges, allBadges, knownAnswers));
        }

        [HttpPost]
        public async Task<IActionResult> Index(LeaderboardModel model)
        {
            var chart = await _apiProvider
                .GetProposalChartAsync()
                .ConfigureAwait(false);

            model.MinimalDate = model.MinimalDate.Date.Max(chart.FirstDate.Date);
            model.MaximalDate = model.MaximalDate.Date.Min(DateTime.Now.Date);
            model.MinimalDate = model.MinimalDate.Min(model.MaximalDate);

            model.Leaders = await _apiProvider
                .GetLeadersAsync(
                    model.SortType,
                    model.MinimalDate,
                    model.MaximalDate)
                .ConfigureAwait(false);

            model.TodayLeaders = await _apiProvider
                .GetTodayLeadersAsync()
                .ConfigureAwait(false);

            return View(model);
        }

        private async Task<IActionResult> Index()
        {
            var chart = await _apiProvider
                .GetProposalChartAsync()
                .ConfigureAwait(false);

            var dateMin = chart.FirstDate.Date;
            var dateMax = DateTime.Now.Date;
            var sortType = LeaderSort.TotalPoints;

            var leaders = await _apiProvider
                .GetLeadersAsync(sortType, dateMin, dateMax)
                .ConfigureAwait(false);

            return View(new LeaderboardModel
            {
                MinimalDate = dateMin,
                MaximalDate = dateMax,
                Leaders = leaders,
                SortType = sortType,
                TodayLeaders = await _apiProvider
                    .GetTodayLeadersAsync()
                    .ConfigureAwait(false)
            });
        }
    }
}
