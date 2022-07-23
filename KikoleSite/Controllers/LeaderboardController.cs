using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Enums;
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
            await SetModelPropertiesAsync(model).ConfigureAwait(false);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("ErrorIndex", "Home");
            }

            var datas = await _apiProvider
                .GetPlayersDistributionAsync(token)
                .ConfigureAwait(false);

            var statsModel = new StatsModel
            {
                DistributionClubs = datas.ClubsDistribution
                    .Select(_ => (_.Rank, _.Count, _.Value.Name, Math.Round(_.Rate, 2)))
                    .ToList(),
                DistributionCountries = datas.CountriesDistribution
                    .Select(_ => (_.Rank, _.Count, _.Value.Name, Math.Round(_.Rate, 2)))
                    .ToList(),
                DistributionDecades = datas.DecadesDistribution
                    .Select(_ => (_.Rank, _.Count, _.Value.ToString(), Math.Round(_.Rate, 2)))
                    .ToList(),
                DistributionPositions = datas.PositionsDistribution
                    .Select(_ => (_.Rank, _.Count, _.Value.GetLabel(), Math.Round(_.Rate, 2)))
                    .ToList()
            };

            return View(statsModel);
        }

        private async Task<IActionResult> IndexInternal(LeaderboardModel model = null)
        {
            model = model ?? new LeaderboardModel();

            model.MinimalDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            model.MaximalDate = DateTime.Now.Date;
            model.SortType = LeaderSorts.TotalPoints;
            model.LeaderboardDay = DateTime.Now.Date;
            model.DaySortType = DayLeaderSorts.BestTime;

            await SetModelPropertiesAsync(model).ConfigureAwait(false);

            return View(model);
        }

        private async Task SetModelPropertiesAsync(LeaderboardModel model)
        {
            model.MinimalDate = model.MinimalDate.Min(model.MaximalDate);

            model.Leaderboard = await _apiProvider
                .GetLeaderboardAsync(model.SortType, model.MinimalDate, model.MaximalDate)
                .ConfigureAwait(false);

            model.Dayboard = await _apiProvider
                .GetDayboardAsync(model.LeaderboardDay.Date, model.DaySortType)
                .ConfigureAwait(false);

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
