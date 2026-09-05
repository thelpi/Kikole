using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Helpers;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers;

public class LeaderboardController : KikoleBaseController
{
    private const string AnonymizedPlayerName = "***";
    private const int DistributionSizeLimit = 25;

    private readonly IStatisticService _statisticService;
    private readonly ILeaderService _leaderService;
    private readonly IProposalService _proposalService;

    public LeaderboardController(IUserRepository userRepository,
        IInternationalService internationalService,
        IClock clock,
        IGameCalendar gameCalendar,
        IPlayerService playerService,
        IBadgeService badgeService,
        ILeaderService leaderService,
        IStatisticService statisticService,
        IProposalService proposalService,
        IHttpContextAccessor httpContextAccessor)
        : base(userRepository,
            internationalService,
            clock,
            gameCalendar,
            playerService,
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
            var model = await InitializeModelAsync();
            return View(model);
        }

        var todayGrant = await _proposalService
            .GetGrantAccessForDayAsync(UserId, _clock.Today);

        var stats = await _leaderService
            .GetUserStatisticsAsync(userId, UserId, AnonymizedPlayerName, todayGrant != DayGrantTypes.None);

        if (stats == null)
        {
            var model = await InitializeModelAsync();
            return View(model);
        }

        var language = ViewHelper.GetLanguage();

        var badges = await _badgeService
             .GetUserBadgesAsync(userId, UserId, language, todayGrant != DayGrantTypes.None);

        var allBadges = await _badgeService
            .GetAllBadgesAsync(language);

        return View("User", new UserStatsModel(stats, badges, allBadges, userId == UserId, _clock));
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public IActionResult Stats()
    {
        return View();
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<JsonResult> GetStatisticPlayersDistribution()
    {
        var datas = await _statisticService
            .GetPlayersDistributionAsync(UserId, ViewHelper.GetLanguage(), DistributionSizeLimit);

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
    [Authorization(UserTypes.Administrator)]
    public async Task<JsonResult> GetStatisticActiveUsers()
    {
        var datas = await _statisticService
            .GetActiveUsersAsync(null, _clock.Yesterday);

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
    [Authorization(UserTypes.Administrator)]
    public async Task<JsonResult> GetKikolesStatisticsAsync([FromQuery] PlayerSorts sort, [FromQuery] bool desc)
    {
        var datas = await _statisticService
            .GetPlayersStatisticsAsync(UserId, AnonymizedPlayerName, sort, desc);

        return Json(datas);
    }

    [HttpGet("global-leaderboard-details")]
    public async Task<JsonResult> GetGlobalLeaderboardDetailsAsync(LeaderSorts sortType, DateTime minimalDate, DateTime maximalDate)
    {
        var (ld, _) = await GetLeaderboardAsync(
                minimalDate, maximalDate, sortType, null);

        return Json(ld);
    }

    [HttpGet("daily-leaderboard-details")]
    public async Task<JsonResult> GetDailyLeaderboardDetailsAsync(DayLeaderSorts sortType, DateTime date)
    {
        var (dailyBoard, _) = await GetDailyboardAsync(
                date, sortType, null);

        return Json(dailyBoard);
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public IActionResult KikolesStats()
    {
        return View("KikolesStats");
    }

    [HttpGet]
    [Authorization]
    public async Task<IActionResult> UserDay(ulong userId, string date)
    {
        if (!DateTime.TryParse(date, out var actualDate)
            || actualDate.Date > _clock.Today
            || actualDate.Date < _gameCalendar.HiddenDate)
        {
            return RedirectToAction("ErrorIndex", "Home");
        }

        var user = await _userRepository
            .GetUserByIdAsync(userId);
        if (user == null || user.UserTypeId == (int)UserTypes.Administrator)
            return RedirectToAction("ErrorIndex", "Home");

        var canSee = await _proposalService
            .GetGrantAccessForDayAsync(UserId, actualDate.Date);

        if (canSee != DayGrantTypes.Creator && canSee != DayGrantTypes.Found && canSee != DayGrantTypes.Admin)
            return RedirectToAction("ErrorIndex", "Home");

        var player = await _playerService
            .GetPlayerOfTheDayFullInfoAsync(actualDate.Date);

        if (player.Player.CreationUserId == userId)
            return RedirectToAction("ErrorIndex", "Home");

        var countryContinents = await _internationalService.GetCountryContinentsAsync();

        var db = await _leaderService
            .GetDayboardAsync(actualDate.Date, DayLeaderSorts.BestTime, countryContinents);

        var proposals = await _proposalService
            .GetProposalsAsync(actualDate.Date, userId, countryContinents);

        var items = new List<UserDayItemModel>(proposals.Count);
        foreach (var proposal in proposals)
        {
            items.Add(new UserDayItemModel
            {
                Date = proposal.Date,
                PointsLost = proposal.PointsLost,
                PointsRemaining = proposal.TotalPoints,
                Success = proposal.Successful,
                Type = proposal.ProposalType,
                Value = proposal.RawValue
            });
        }

        // le dernier point restant si le classement n'a pas encore de score enregistre
        // pour ce jour (ex. abandon), sinon BasePoints faute de proposition du tout
        var lastKnownPoints = proposals.Count > 0
            ? proposals.Last().TotalPoints
            : ScoreCalculator.BasePoints;

        var model = new UserDayModel
        {
            ProposalDate = actualDate.Date,
            PlayerName = player.Player.Name,
            UserLogin = user.Login,
            ProposalDetails = items,
            UserScore = db.Leaders.FirstOrDefault(_ => _.UserId == userId)?.Points ?? lastKnownPoints
        };

        return View("UserDay", model);
    }

    /// <summary>
    /// Modele par defaut du classement : le mois courant, trie par points cumules.
    /// </summary>
    private async Task<LeaderboardModel> InitializeModelAsync()
    {
        var (dailyBoard, foundToday) = await GetDailyboardAsync(
                _clock.Today, DayLeaderSorts.BestTime, null);

        var (globalLeaderboard, _) = await GetLeaderboardAsync(
                _clock.FirstOfMonth, _clock.Today, LeaderSorts.TotalPoints, foundToday);

        var palmares = await _leaderService.GetPalmaresAsync();

        return new LeaderboardModel
        {
            MinimalDate = _clock.FirstOfMonth,
            MaximalDate = _clock.Today,
            SortType = LeaderSorts.TotalPoints,
            LeaderboardDay = _clock.Today,
            DaySortType = DayLeaderSorts.BestTime,
            Dayboard = dailyBoard,
            GlobalLeaderboard = globalLeaderboard,
            CurrentUserId = UserId,
            MonthlyPodiums = palmares.MonthlyPalmares
                .Select(x => (
                    new DateTime(x.Key.year, x.Key.month, 1),
                    new[]
                    {
                        (x.Value.first.Id, x.Value.first.Login),
                        (x.Value.second.Id, x.Value.second.Login),
                        (x.Value.third.Id, x.Value.third.Login)
                    }))
                .ToList(),
            OverallPodium = palmares.GlobalPalmares
                .Select(x => (x.user.Id, x.user.Login, x.first, x.second, x.third))
                .ToList()
        };
    }

    private async Task<(IReadOnlyCollection<Models.LeaderboardItem>, DayGrantTypes)> GetLeaderboardAsync(
        DateTime minDate, DateTime maxDate, LeaderSorts sortType, DayGrantTypes? todayGrant)
    {
        var todayGrantEnsured = todayGrant ?? await _proposalService
            .GetGrantAccessForDayAsync(UserId, _clock.Today);

        // this case usually happens the first of the month
        // the former code switch to the previous month
        if (todayGrantEnsured == DayGrantTypes.None
            && minDate >= _clock.Today
            && maxDate >= _clock.Today)
        {
            return (new List<Models.LeaderboardItem>(), todayGrantEnsured);
        }

        minDate = await EnsureDateAsync(minDate, todayGrantEnsured);
        maxDate = await EnsureDateAsync(maxDate, todayGrantEnsured);

        if (maxDate < minDate)
        {
            var swap = minDate;
            minDate = maxDate;
            maxDate = swap;
        }

        var board = await _leaderService
            .GetLeaderboardAsync(minDate, maxDate, sortType);

        return (board, todayGrantEnsured);
    }

    private async Task<(Models.Dayboard, DayGrantTypes)> GetDailyboardAsync(
        DateTime date, DayLeaderSorts sortType, DayGrantTypes? todayGrant)
    {
        var todayGrantEnsured = todayGrant ?? await _proposalService
            .GetGrantAccessForDayAsync(UserId, _clock.Today);

        date = await EnsureDateAsync(date, DayGrantTypes.Found); // any DayGrantTypes but "None"

        Dayboard dayboard;
        if (date == _clock.Today && todayGrantEnsured == DayGrantTypes.None)
        {
            dayboard = new Dayboard
            {
                Date = date,
                Sort = sortType,
                Hidden = true,
                // le tableau est masque : collections vides plutot que nulles, pour
                // qu'un appelant qui oublierait de tester Hidden ne casse pas
                Leaders = [],
                Searchers = []
            };
        }
        else
        {
            dayboard = await _leaderService
                .GetDayboardAsync(date, sortType, await _internationalService.GetCountryContinentsAsync());
        }

        return (dayboard, todayGrantEnsured);
    }

    private async Task<DateTime> EnsureDateAsync(DateTime date, DayGrantTypes todayGrant)
    {
        if (date.Date > _clock.Today)
        {
            date = _clock.Today;
        }

        if (todayGrant == DayGrantTypes.None && date.Date == _clock.Today)
        {
            date = _clock.Yesterday;
        }

        if (date.Date <= _gameCalendar.HiddenDate)
        {
            date = _gameCalendar.HiddenDate;
            var displayHidden = await _playerService
                .CanDisplayHiddenPlayerAsync(UserId);
            if (!displayHidden)
                date = _gameCalendar.FirstDate;
        }

        return date.Date;
    }
}
