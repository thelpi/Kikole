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
            var (token, login) = GetAuthenticationCookie();
            var isLogged = !string.IsNullOrWhiteSpace(token);

            if (userId == 0 || !isLogged)
            {
                return await IndexInternal(null, isLogged).ConfigureAwait(false);
            }

            var stats = await _apiProvider
                .GetUserStatsAsync(userId)
                .ConfigureAwait(false);

            if (stats == null)
            {
                return await IndexInternal(null, isLogged).ConfigureAwait(false);
            }

            var today = DateTime.Now.Date;

            var challenge = (await _apiProvider
                    .GetAcceptedChallengesAsync(token)
                    .ConfigureAwait(false))
                .SingleOrDefault(c => c.ChallengeDate == today);

            if (challenge?.OpponentLogin == stats.Login)
            {
                var todayLeaders = await _apiProvider
                    .GetDayLeadersAsync(today, LeaderSort.TotalPoints)
                    .ConfigureAwait(false);
                if (!todayLeaders.Any(tl => tl.Login == login))
                    return await IndexInternal(null, isLogged).ConfigureAwait(false);
            }

            var badges = await _apiProvider
                .GetUserBadgesAsync(userId, token)
                .ConfigureAwait(false);

            var allBadges = await _apiProvider
                .GetBadgesAsync()
                .ConfigureAwait(false);

            var knownAnswers = new List<string>();
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
            var (token, login) = GetAuthenticationCookie();
            var isLogged = !string.IsNullOrWhiteSpace(token);

            model.IsLogged = isLogged;

            if (isLogged)
            {
                var chart = await _apiProvider
                    .GetProposalChartAsync()
                    .ConfigureAwait(false);

                await SetModelPropertiesAsync(
                        model, chart.FirstDate)
                    .ConfigureAwait(false);
            }

            return View(model);
        }

        private async Task SetModelPropertiesAsync(
            LeaderboardModel model,
            DateTime firstDate)
        {
            model.MinimalDate = model.MinimalDate.Date.Max(firstDate.Date);
            model.MaximalDate = model.MaximalDate.Date.Min(DateTime.Now.Date);
            model.MinimalDate = model.MinimalDate.Min(model.MaximalDate);

            var day = model.LeaderboardDay.Date.Max(firstDate);

            var dayleaders = await _apiProvider
                .GetDayLeadersAsync(day, model.DaySortType)
                .ConfigureAwait(false);

            var leaders = await _apiProvider
                .GetLeadersAsync(model.SortType, model.MinimalDate, model.MaximalDate)
                .ConfigureAwait(false);

            model.Leaders = leaders;
            model.TodayLeaders = dayleaders;
        }

        private async Task<IActionResult> IndexInternal(LeaderboardModel model, bool isLogged)
        {
            model = model ?? new LeaderboardModel();
            model.IsLogged = isLogged;

            if (model.IsLogged)
            {
                var chart = await _apiProvider
                    .GetProposalChartAsync()
                    .ConfigureAwait(false);

                model.MinimalDate = chart.FirstDate.Date;
                model.MaximalDate = DateTime.Now.Date;
                model.SortType = LeaderSort.TotalPoints;
                model.LeaderboardDay = DateTime.Now.Date;
                model.DaySortType = LeaderSort.BestTime;

                await SetModelPropertiesAsync(
                        model, chart.FirstDate)
                    .ConfigureAwait(false);
            }

            return View(model);
        }
    }
}
