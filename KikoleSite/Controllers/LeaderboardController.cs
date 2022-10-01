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
        public IActionResult Stats()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetStatisticPlayersDistribution()
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Json(new {});
            }

            var datas = await _apiProvider
                .GetPlayersDistributionAsync(token)
                .ConfigureAwait(false);

            return Json(new
            {
                country = datas.CountriesDistribution.Select(_ =>
                    new KeyValuePair<string, decimal>(_.Value.Name, Math.Round(_.Rate, 2))),
                decade = datas.DecadesDistribution.Select(_ =>
                    new KeyValuePair<string, decimal>(_.Value.ToString(), Math.Round(_.Rate, 2))),
                position = datas.PositionsDistribution.Select(_ =>
                    new KeyValuePair<string, decimal>(_.Value.GetLabel(), Math.Round(_.Rate, 2))),
                club = datas.ClubsDistribution.Select(_ =>
                    new KeyValuePair<string, decimal>(_.Value.Name, _.Count))
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetStatisticActiveUsers()
        {
            var datas = await _apiProvider
                .GetStatisticActiveUsersAsync()
                .ConfigureAwait(false);

            return Json(new
            {
                monthly = datas.MonthlyDatas.Select(_ =>
                    new KeyValuePair<string, int>($"{_.Key.m.ToString().PadLeft(2, '0')} ({_.Key.y.ToString().Substring(2, 2)})", _.Value)),
                weekly = datas.WeeklyDatas.Select(_ =>
                    new KeyValuePair<string, int>($"{_.Key.w.ToString().PadLeft(2, '0')} ({_.Key.y.ToString().Substring(2, 2)})", _.Value)),
                daily = datas.DailyDatas.Select(_ =>
                    new KeyValuePair<string, int>(_.Key.GetNumDayLabel(), _.Value))
            });
        }

        private async Task<IActionResult> IndexInternal(LeaderboardModel model = null)
        {
            model ??= new LeaderboardModel();

            model.MinimalDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            model.MaximalDate = DateTime.Now.Date;
            model.SortType = LeaderSorts.TotalPoints;
            model.LeaderboardDay = DateTime.Now.Date;
            model.DaySortType = DayLeaderSorts.BestTime;

            await SetModelPropertiesAsync(model).ConfigureAwait(false);

            return View(model);
        }

        [HttpGet("leaderboard-details")]
        public async Task<JsonResult> GetLeaderboardDetailsAsync(LeaderSorts sortType, DateTime minimalDate, DateTime maximalDate)
        {
            var ld = await _apiProvider
                .GetLeaderboardAsync(sortType, minimalDate, maximalDate)
                .ConfigureAwait(false);

            return Json(ld);
        }

        [HttpGet]
        public async Task<IActionResult> Palmares()
        {
            var model = new PalmaresModel();

            var data = await _apiProvider
                .GetPalmaresAsync()
                .ConfigureAwait(false);

            model.MonthlyPalmares = data.MonthlyPalmares
                .Select(x => (
                    new DateTime(x.Key.year, x.Key.month, 1),
                    new[]
                    {
                        (x.Value.first.Id, x.Value.first.Login),
                        (x.Value.second.Id, x.Value.second.Login),
                        (x.Value.third.Id, x.Value.third.Login)
                    }))
                .ToList();
            model.GlobalPalmares = data.GlobalPalmares
                .Select(x => (x.user.Login, x.first, x.second, x.third))
                .ToList();

            return View("Palmares", model);
        }

        private async Task SetModelPropertiesAsync(LeaderboardModel model)
        {
            model.MinimalDate = model.MinimalDate.Min(model.MaximalDate);

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
