using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers
{
    public class LeaderboardController : KikoleBaseController
    {
        private readonly IStringLocalizer<LeaderboardController> _localizer;

        public LeaderboardController(IApiProvider apiProvider, IStringLocalizer<LeaderboardController> localizer)
            : base(apiProvider)
        {
            _localizer = localizer;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ulong userId)
        {
            if (userId == 0)
            {
                return await IndexInternal().ConfigureAwait(false);
            }

            var stats = await _apiProvider
                .GetUserStatsAsync(userId)
                .ConfigureAwait(false);

            if (stats == null)
            {
                return await IndexInternal().ConfigureAwait(false);
            }

            var (token, login) = GetAuthenticationCookie();

            var badges = await _apiProvider
                .GetUserBadgesAsync(userId, token)
                .ConfigureAwait(false);

            var allBadges = (await _apiProvider
                .GetBadgesAsync()
                .ConfigureAwait(false))
                .ToList();

            var knownAnswers = new List<string>();
            if (!string.IsNullOrWhiteSpace(token))
            {
                knownAnswers = (await _apiProvider
                    .GetUserKnownPlayersAsync(token)
                    .ConfigureAwait(false))
                    .ToList();
            }

            return View("User", new UserStatsModel(stats, badges, allBadges, knownAnswers, login == stats.Login));
        }

        [HttpPost]
        public async Task<IActionResult> Index(LeaderboardModel model)
        {
            var chart = await _apiProvider
                .GetProposalChartAsync()
                .ConfigureAwait(false);

            await SetModelPropertiesAsync(
                    model, chart.FirstDate)
                .ConfigureAwait(false);

            return View(model);
        }

        private async Task<IActionResult> IndexInternal(LeaderboardModel model = null)
        {
            model = model ?? new LeaderboardModel();

            var chart = await _apiProvider
                .GetProposalChartAsync()
                .ConfigureAwait(false);

            model.MinimalDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            model.MaximalDate = DateTime.Now.Date;
            model.SortType = LeaderSort.TotalPoints;
            model.LeaderboardDay = DateTime.Now.Date;
            model.DaySortType = DayLeaderSort.BestTime;

            await SetModelPropertiesAsync(
                    model, chart.FirstDate)
                .ConfigureAwait(false);

            return View(model);
        }

        private async Task SetModelPropertiesAsync(
            LeaderboardModel model,
            DateTime firstDate)
        {
            model.MinimalDate = model.MinimalDate.Min(model.MaximalDate);

            var day = model.LeaderboardDay.Date.Max(firstDate.AddDays(-1)); // inc. secret

            var dayleaders = await _apiProvider
                .GetDayLeadersAsync(day, model.DaySortType)
                .ConfigureAwait(false);

            var leaders = await _apiProvider
                .GetLeadersAsync(model.SortType, model.MinimalDate, model.MaximalDate, model.IncludePvp)
                .ConfigureAwait(false);

            var users = await _apiProvider
                .GetUsersWithProposalAsync(day)
                .ConfigureAwait(false);

            model.Leaders = leaders;
            model.TodayLeaders = dayleaders;
            model.TodayAttemps = users.Count;

            // TODO: meilleure solution à venir
            if (DateTime.Now.Day <= 3)
            {
                model.Awards = await _apiProvider
                    .GetMonthlyAwardsAsync(DateTime.Now.Year, DateTime.Now.Month - 1)
                    .ConfigureAwait(false);
            }

            model.BoardName = _localizer["CustomLeaderboard"];
            var isCurrentMonthStart = model.MinimalDate.IsFirstOfMonth();
            var isCurrentMonthEnd = model.MaximalDate.IsAfterInMonth();
            if (isCurrentMonthStart && isCurrentMonthEnd)
            {
                model.BoardName = _localizer["MonthLeaderboard"];
            }
            else
            {
                var isMonthStart = model.MinimalDate.IsFirstOfMonth(model.MinimalDate);
                var isMonthEnd = model.MaximalDate.IsEndOfMonth(model.MinimalDate);
                if (isMonthStart && isMonthEnd)
                {
                    model.BoardName = _localizer["MonthNameLeaderboard", model.MinimalDate.GetMonthName()];
                }
                else if (model.MaximalDate.Date == DateTime.Now.Date)
                {
                    model.BoardName = _localizer["LastDaysLeaderboard", Convert.ToInt32(Math.Floor((model.MaximalDate.Date - model.MinimalDate.Date).TotalDays))];
                }
            }
        }
    }
}
