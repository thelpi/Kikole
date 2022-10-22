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
        private readonly IProposalService _proposalService;

        public LeaderboardController(IUserRepository userRepository,
            ICrypter crypter,
            IInternationalRepository internationalRepository,
            IClock clock,
            IPlayerService playerService,
            IClubRepository clubRepository,
            IBadgeService badgeService,
            ILeaderService leaderService,
            IStatisticService statisticService,
            IProposalService proposalService,
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
            _proposalService = proposalService;
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

            var foundToday = await _proposalService
                .CanSeeTodayLeaderboardAsync(UserId)
                .ConfigureAwait(false);

            var stats = await _leaderService
                .GetUserStatisticsAsync(userId, UserId, AnonymizedPlayerName, foundToday)
                .ConfigureAwait(false);

            if (stats == null)
            {
                var model = await InitializeModelAsync(null).ConfigureAwait(false);
                return View(model);
            }

            var language = ViewHelper.GetLanguage();

            var badges = await _badgeService
                 .GetUserBadgesAsync(userId, UserId, language, foundToday)
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
                .GetActiveUsersAsync(null, _clock.Yesterday)
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
            var (ld, _) = await GetLeaderboardAsync(
                    minimalDate, maximalDate, sortType, null)
                .ConfigureAwait(false);

            return Json(ld);
        }

        [HttpGet("daily-leaderboard-details")]
        public async Task<JsonResult> GetDailyLeaderboardDetailsAsync(DayLeaderSorts sortType, DateTime date)
        {
            var (dailyBoard, _) = await GetDailyboardAsync(
                    date, sortType, null)
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
                    MaximalDate = _clock.Today,
                    SortType = LeaderSorts.TotalPoints,
                    LeaderboardDay = _clock.Today,
                    DaySortType = DayLeaderSorts.BestTime
                };
            }

            var (dailyBoard, foundToday) = await GetDailyboardAsync(
                    model.LeaderboardDay, model.DaySortType, null)
                .ConfigureAwait(false);

            model.Dayboard = dailyBoard;

            (model.GlobalLeaderboard, _) = await GetLeaderboardAsync(
                    model.MinimalDate, model.MaximalDate, model.SortType, foundToday)
                .ConfigureAwait(false);

            return model;
        }

        private async Task<(IReadOnlyCollection<Models.LeaderboardItem>, bool)> GetLeaderboardAsync(
            DateTime minDate, DateTime maxDate, LeaderSorts sortType, bool? foundToday)
        {
            var foundTodayEnsured = foundToday ?? await _proposalService
                .CanSeeTodayLeaderboardAsync(UserId)
                .ConfigureAwait(false);

            minDate = EnsureDate(minDate, foundTodayEnsured);
            maxDate = EnsureDate(maxDate, foundTodayEnsured);

            if (maxDate < minDate)
            {
                var swap = minDate;
                minDate = maxDate;
                maxDate = swap;
            }

            var board = await _leaderService
                .GetLeaderboardAsync(minDate, maxDate, sortType)
                .ConfigureAwait(false);

            return (board, foundTodayEnsured);
        }

        private async Task<(Models.Dayboard, bool)> GetDailyboardAsync(
            DateTime date, DayLeaderSorts sortType, bool? foundToday)
        {
            var foundTodayEnsured = foundToday ?? await _proposalService
                .CanSeeTodayLeaderboardAsync(UserId)
                .ConfigureAwait(false);

            date = EnsureDate(date, true);

            Models.Dayboard dayboard;
            if (date == _clock.Today && !foundTodayEnsured)
            {
                dayboard = new Models.Dayboard
                {
                    Date = date,
                    Sort = sortType,
                    Hidden = true
                };
            }
            else
            {
                dayboard = await _leaderService
                    .GetDayboardAsync(date, sortType)
                    .ConfigureAwait(false);
            }

            return (dayboard, foundTodayEnsured);
        }

        private DateTime EnsureDate(DateTime date, bool foundToday)
        {
            if (date.Date > _clock.Today)
            {
                date = _clock.Today;
            }

            if (!foundToday && date.Date == _clock.Today)
            {
                date = _clock.Yesterday;
            }

            return date.Date;
        }
    }
}
