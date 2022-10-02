using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Interfaces.Services;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;
using KikoleSite.Api.Models.Statistics;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers
{
    public class LeaderboardController : KikoleBaseController
    {
        private readonly IStringLocalizer<LeaderboardController> _localizer;
        private readonly IStatisticService _statisticService;
        private readonly ILeaderService _leaderService;

        public LeaderboardController(IStringLocalizer<LeaderboardController> localizer,
            IUserRepository userRepository,
            ICrypter crypter,
            IStringLocalizer<Translations> resources,
            IInternationalRepository internationalRepository,
            IMessageRepository messageRepository,
            IClock clock,
            IPlayerService playerService,
            IClubRepository clubRepository,
            IBadgeService badgeService,
            ILeaderService leaderService,
            IStatisticService statisticService)
            : base(userRepository,
                crypter,
                resources,
                internationalRepository,
                messageRepository,
                clock,
                playerService,
                clubRepository,
                badgeService)
        {
            _localizer = localizer;
            _statisticService = statisticService;
            _leaderService = leaderService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ulong userId)
        {
            if (userId == 0)
            {
                return await IndexInternal().ConfigureAwait(false);
            }

            var stats = await GetUserStatsAsync(
                    userId)
                .ConfigureAwait(false);

            if (stats == null)
            {
                return await IndexInternal().ConfigureAwait(false);
            }

            var (token, login) = GetAuthenticationCookie();

            var badges = await GetUserBadgesAsync(
                    userId, token)
                .ConfigureAwait(false);

            var allBadges = await GetBadgesAsync().ConfigureAwait(false);

            IReadOnlyCollection<string> knownAnswers = new List<string>();
            if (!string.IsNullOrWhiteSpace(token))
            {
                knownAnswers = await GetUserKnownPlayersAsync(
                        token)
                    .ConfigureAwait(false);
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
                return Json(new { });
            }

            var datas = await GetPlayersDistributionAsync(
                    token)
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
            var datas = await GetStatisticActiveUsersAsync().ConfigureAwait(false);

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
            var ld = await GetLeaderboardAsync(
                    sortType, minimalDate, maximalDate)
                .ConfigureAwait(false);

            return Json(ld);
        }

        [HttpGet]
        public async Task<IActionResult> Palmares()
        {
            var model = new PalmaresModel();

            var data = await GetPalmaresAsync().ConfigureAwait(false);

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

            model.Dayboard = await GetDayboardAsync(
                    model.LeaderboardDay.Date, model.DaySortType)
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

        private async Task<Palmares> GetPalmaresAsync()
        {
            return await _leaderService
                .GetPalmaresAsync()
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyCollection<LeaderboardItem>> GetLeaderboardAsync(LeaderSorts leaderSort, DateTime minimalDate, DateTime maximalDate)
        {
            return await _leaderService
                .GetLeaderboardAsync(minimalDate, maximalDate, leaderSort)
                .ConfigureAwait(false);
        }

        private async Task<Dayboard> GetDayboardAsync(DateTime day, DayLeaderSorts sort)
        {
            return await _leaderService
                .GetDayboardAsync(day, sort)
                .ConfigureAwait(false);
        }

        private async Task<UserStat> GetUserStatsAsync(ulong id)
        {
            if (id == 0)
                return null;

            var userStatistics = await _leaderService
                .GetUserStatisticsAsync(id)
                .ConfigureAwait(false);

            if (userStatistics == null)
                return null;

            return userStatistics;
        }

        private async Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(ulong userId, string authToken)
        {
            var connectedUserId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            var badgesFull = await _badgeService
                 .GetUserBadgesAsync(userId, connectedUserId, Helper.GetLanguage())
                 .ConfigureAwait(false);

            return badgesFull;
        }

        private async Task<IReadOnlyCollection<Badge>> GetBadgesAsync()
        {
            return await _badgeService
                .GetAllBadgesAsync(Helper.GetLanguage())
                .ConfigureAwait(false);
        }

        private async Task<PlayersDistribution> GetPlayersDistributionAsync(string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            return await _statisticService
                .GetPlayersDistributionAsync(userId, Helper.GetLanguage(), 25)
                .ConfigureAwait(false);
        }

        private async Task<ActiveUsers> GetStatisticActiveUsersAsync()
        {
            return await _statisticService
                .GetActiveUsersAsync()
                .ConfigureAwait(false);
        }
    }
}
