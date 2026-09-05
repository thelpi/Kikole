using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Helpers;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers;

public class StatisticsController : KikoleBaseController
{
    private const string AnonymizedPlayerName = "***";
    private const int DistributionSizeLimit = 25;

    private readonly IStatisticService _statisticService;

    public StatisticsController(IUserRepository userRepository,
        IInternationalService internationalService,
        IClock clock,
        IGameCalendar gameCalendar,
        IPlayerService playerService,
        IBadgeService badgeService,
        IStatisticService statisticService,
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
}
