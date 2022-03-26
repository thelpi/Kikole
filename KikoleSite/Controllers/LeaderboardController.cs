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

            var (token, login) = GetAuthenticationCookie();

            var today = DateTime.Now.Date;

            var challenge = await _apiProvider
                .GetAcceptedChallengeAsync(today, token)
                .ConfigureAwait(false);

            if (challenge?.OpponentLogin == stats.Login)
            {
                var todayLeaders = await _apiProvider
                    .GetDayLeadersAsync(today, LeaderSort.TotalPoints)
                    .ConfigureAwait(false);
                if (!todayLeaders.Any(tl => tl.Login == login))
                    return await Index().ConfigureAwait(false);
            }

            var badges = await _apiProvider
                .GetUserBadgesAsync(userId)
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
            var chart = await _apiProvider
                .GetProposalChartAsync()
                .ConfigureAwait(false);

            model.MinimalDate = model.MinimalDate.Date.Max(chart.FirstDate.Date);
            model.MaximalDate = model.MaximalDate.Date.Min(DateTime.Now.Date);
            model.MinimalDate = model.MinimalDate.Min(model.MaximalDate);

            var leaders = await _apiProvider
                .GetLeadersAsync(model.SortType, model.MinimalDate, model.MaximalDate)
                .ConfigureAwait(false);

            var day = model.LeaderboardDay.Date.Max(chart.FirstDate);

            var dayleaders = await _apiProvider
                .GetDayLeadersAsync(day, model.DaySortType)
                .ConfigureAwait(false);

            var (token, login) = GetAuthenticationCookie();

            var challenge = await _apiProvider
                .GetAcceptedChallengeAsync(DateTime.Now.Date, token)
                .ConfigureAwait(false);

            model.Leaders = model.MaximalDate.IsToday()
                ? AnonymizeLeaders(leaders, challenge, login)
                : leaders;
            model.TodayLeaders = day.IsToday()
                ? AnonymizeLeaders(dayleaders, challenge, login)
                : dayleaders;

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

            var day = DateTime.Now.Date;
            var daySort = LeaderSort.BestTime;

            var leaders = await _apiProvider
                .GetLeadersAsync(sortType, dateMin, dateMax)
                .ConfigureAwait(false);

            var todayLeaders = await _apiProvider
                .GetDayLeadersAsync(day, daySort)
                .ConfigureAwait(false);

            var (token, login) = GetAuthenticationCookie();

            var challenge = await _apiProvider
                .GetAcceptedChallengeAsync(DateTime.Now.Date, token)
                .ConfigureAwait(false);

            return View(new LeaderboardModel
            {
                MinimalDate = dateMin,
                MaximalDate = dateMax,
                Leaders = AnonymizeLeaders(leaders, challenge, login),
                SortType = sortType,
                TodayLeaders = AnonymizeLeaders(todayLeaders, challenge, login),
                LeaderboardDay = day,
                DaySortType = daySort
            });
        }

        private IReadOnlyCollection<Leader> AnonymizeLeaders(
            IReadOnlyCollection<Leader> leaders, Challenge myChallenge, string myLogin)
        {
            if (myChallenge == null || leaders.Any(l => l.Login == myLogin))
                return leaders;

            var keepLeaders = new List<Leader>(leaders.Count);
            int? excludePosition = null;
            foreach (var l in leaders)
            {
                if (l.Login == myChallenge.OpponentLogin)
                    excludePosition = l.Position;
                else
                {
                    l.Position = excludePosition.HasValue && l.Position > excludePosition
                        ? l.Position - 1
                        : l.Position;
                    keepLeaders.Add(l);
                }
            }

            return keepLeaders;
        }
    }
}
