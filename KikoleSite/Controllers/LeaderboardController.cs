using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Helpers;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class LeaderboardController : KikoleBaseController
    {
        private const string AnonymizedPlayerName = "***";
        private const int DistributionSizeLimit = 25;

        private readonly IStatisticService _statisticService;
        private readonly ILeaderService _leaderService;

        public LeaderboardController(IUserRepository userRepository,
            ICrypter crypter,
            IInternationalRepository internationalRepository,
            IClock clock,
            IPlayerService playerService,
            IClubRepository clubRepository,
            IBadgeService badgeService,
            ILeaderService leaderService,
            IStatisticService statisticService,
            IHttpContextAccessor httpContextAccessor)
            : base(userRepository,
                crypter,
                internationalRepository,
                clock,
                playerService,
                clubRepository,
                badgeService,
                httpContextAccessor)
        {
            _statisticService = statisticService;
            _leaderService = leaderService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ulong userId)
        {
            // /!\ userId is not UserId
            if (userId == 0)
            {
                var model = await InitializeModelAsync(null).ConfigureAwait(false);
                return View(model);
            }

            var stats = await _leaderService
                .GetUserStatisticsAsync(userId, UserId, AnonymizedPlayerName)
                .ConfigureAwait(false);

            if (stats == null)
            {
                var model = await InitializeModelAsync(null).ConfigureAwait(false);
                return View(model);
            }

            var language = ViewHelper.GetLanguage();

            var badges = await _badgeService
                 .GetUserBadgesAsync(userId, UserId, language)
                 .ConfigureAwait(false);

            var allBadges = await _badgeService
                .GetAllBadgesAsync(language)
                .ConfigureAwait(false);

            return View("User", new UserStatsModel(stats, badges, allBadges, userId == UserId));
        }

        [HttpGet]
        public IActionResult Stats()
        {
            return View();
        }

        [HttpGet]
        [Authorization]
        public async Task<JsonResult> GetStatisticPlayersDistribution()
        {
            var datas = await _statisticService
                .GetPlayersDistributionAsync(UserId, ViewHelper.GetLanguage(), DistributionSizeLimit)
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
            var datas = await _statisticService
                .GetActiveUsersAsync()
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

        [HttpGet("kikoles-stats")]
        [Authorization]
        public async Task<JsonResult> GetKikolesStatisticsAsync([FromQuery] PlayerSorts sort, [FromQuery] bool desc)
        {
            var datas = await _statisticService
                .GetPlayersStatisticsAsync(UserId, AnonymizedPlayerName, sort, desc)
                .ConfigureAwait(false);

            return Json(datas);
        }

        [HttpGet("global-leaderboard-details")]
        public async Task<JsonResult> GetGlobalLeaderboardDetailsAsync(LeaderSorts sortType, DateTime minimalDate, DateTime maximalDate)
        {
            var ld = await _leaderService
                .GetLeaderboardAsync(minimalDate, maximalDate, sortType)
                .ConfigureAwait(false);

            return Json(ld);
        }

        [HttpGet("daily-leaderboard-details")]
        public async Task<JsonResult> GetDailyLeaderboardDetailsAsync(DayLeaderSorts sortType, DateTime date)
        {
            var dailyBoard = await _leaderService
                .GetDayboardAsync(date, sortType)
                .ConfigureAwait(false);

            return Json(dailyBoard);
        }

        [HttpGet]
        public async Task<IActionResult> Palmares()
        {
            var data = await _leaderService
                .GetPalmaresAsync()
                .ConfigureAwait(false);

            return View("Palmares", new PalmaresModel(data));
        }

        [HttpGet]
        [Authorization]
        public IActionResult KikolesStats()
        {
            return View("KikolesStats");
        }

        private async Task<LeaderboardModel> InitializeModelAsync(LeaderboardModel model)
        {
            if (model == null)
            {
                model = new LeaderboardModel
                {
                    MinimalDate = _clock.FirstOfMonth,
                    MaximalDate = DateTime.Now.Date,
                    SortType = LeaderSorts.TotalPoints,
                    LeaderboardDay = DateTime.Now.Date,
                    DaySortType = DayLeaderSorts.BestTime
                };
            }

            model.MinimalDate = model.MinimalDate.Min(model.MaximalDate);

            model.Dayboard = await _leaderService
                .GetDayboardAsync(model.LeaderboardDay.Date, model.DaySortType)
                .ConfigureAwait(false);

            model.GlobalLeaderboard = await _leaderService
                .GetLeaderboardAsync(model.MinimalDate, model.MaximalDate, model.SortType)
                .ConfigureAwait(false);

            return model;
        }
    }
}
