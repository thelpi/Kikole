using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KikoleSite.Controllers
{
    public class HomeController : KikoleBaseController
    {
        const ulong DefaultLanguageId = 1;

        private readonly IApiProvider _apiProvider;

        private static readonly Dictionary<ulong, IReadOnlyDictionary<ulong, string>> _countriesCache
             = new Dictionary<ulong, IReadOnlyDictionary<ulong, string>>();
        private static ProposalChart _proposalChartCache;
        private static (IReadOnlyCollection<Club> clubs, DateTime expiration) _clubsCache;

        public HomeController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
            // force cache
            GetClubs();
        }

        public async Task<IActionResult> Index(
            [FromQuery] int? day, [FromQuery] string errorMessageForced)
        {
            var (token, login) = this.GetAuthenticationCookie();

            var chart = GetProposalChartCache();

            var model = new HomeModel { Points = chart.BasePoints };
            
            if (day.HasValue
                && model.CurrentDay != day.Value
                && day.Value >= 0
                && DateTime.Now.Date.AddDays(-day.Value) >= chart.FirstDate)
            {
                model = new HomeModel
                {
                    Points = chart.BasePoints,
                    CurrentDay = day.Value
                };
            }

            var proposalDate = DateTime.Now.Date.AddDays(-model.CurrentDay);

            await SetModelFromApiAsync(model, proposalDate, token).ConfigureAwait(false);

            var clue = await _apiProvider.GetClueAsync(proposalDate).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(errorMessageForced))
            {
                model.IsErrorMessage = true;
                model.MessageToDisplay = errorMessageForced;
            }

            return ViewWithFullModel(model, login, clue, chart);
        }

        [HttpPost]
        public async Task<IActionResult> Index(HomeModel model)
        {
            var (token, login) = this.GetAuthenticationCookie();

            if (model == null
                || !Enum.TryParse<ProposalType>(GetSubmitAction(), out var proposalType)
                || string.IsNullOrWhiteSpace(token))
            {
                return Redirect("/");
            }

            var value = model.GetValueFromProposalType(proposalType);
            if (string.IsNullOrWhiteSpace(value))
            {
                return await Index(model.CurrentDay, "Invalid request")
                    .ConfigureAwait(false);
            }

            var now = DateTime.Now;

            var response = await _apiProvider
                .SubmitProposalAsync(now, value, model.CurrentDay,
                    proposalType,
                    token,
                    model.Points)
                .ConfigureAwait(false);

            model.IsErrorMessage = !response.Successful;
            model.MessageToDisplay = (response.Successful
                ? $"Valid {proposalType} guess"
                : $"Invalid {proposalType} guess{(!string.IsNullOrWhiteSpace(response.Tip) ? $"; {response.Tip}" : "")}");

            var proposalDate = now.Date.AddDays(-model.CurrentDay);

            await SetModelFromApiAsync(model, proposalDate, token).ConfigureAwait(false);

            var clue = await _apiProvider.GetClueAsync(proposalDate).ConfigureAwait(false);

            return ViewWithFullModel(model, login, clue, GetProposalChartCache());
        }

        [HttpPost]
        public JsonResult AutoCompleteCountries(string prefix, ulong languageId = DefaultLanguageId)
        {
            var countries = GetCountries()
                .Where(c =>
                    c.Value.ToLowerInvariant().Contains(prefix.ToLowerInvariant()));

            return Json(countries);
        }

        [HttpPost]
        public JsonResult AutoCompleteClubs(string prefix)
        {
            var clubs = GetClubs()
                .Where(c =>
                    c.Name.ToLowerInvariant().Contains(prefix.ToLowerInvariant()));

            return Json(clubs.Select(x => x.Name));
        }

        private IActionResult ViewWithFullModel(
            HomeModel model, string login, string clue, ProposalChart chart)
        {
            model.LoggedAs = login;
            model.Positions = new[] { new SelectListItem("", "0") }
                .Concat(GetPositions()
                    .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
                .ToList();
            model.Chart = GetProposalChartCache();
            model.Clue = clue;
            model.NoPreviousDay = DateTime.Now.Date.AddDays(-model.CurrentDay) == chart.FirstDate;
            return View(model);
        }

        private IReadOnlyCollection<Club> GetClubs()
        {
            if (_clubsCache.clubs == null || _clubsCache.expiration < DateTime.Now)
            {
                var clubs = _apiProvider
                    .GetClubsAsync().GetAwaiter()
                    .GetResult();

                _clubsCache = (clubs.OrderBy(c => c.Name).ToList(), DateTime.Now.AddHours(1));
            }

            return _clubsCache.clubs;
        }

        private IReadOnlyDictionary<ulong, string> GetPositions()
        {
            return Enum
                .GetValues(typeof(Position))
                .Cast<Position>()
                .ToDictionary(_ => (ulong)_, _ => _.ToString());
        }

        private IReadOnlyDictionary<ulong, string> GetCountries(ulong languageId = DefaultLanguageId)
        {
            if (!_countriesCache.ContainsKey(languageId))
            {
                // synchronous
                var apiCountries = _apiProvider
                    .GetCountriesAsync(languageId)
                    .GetAwaiter()
                    .GetResult()
                    .OrderBy(ac => ac.Name)
                    .ToDictionary(ac => ac.Code, ac => ac.Name);
                _countriesCache.Add(languageId, apiCountries);
            }

            return _countriesCache[languageId];
        }

        private ProposalChart GetProposalChartCache()
        {
            return _proposalChartCache ??
                (_proposalChartCache = _apiProvider.GetProposalChartAsync().GetAwaiter().GetResult());
        }

        private async Task SetModelFromApiAsync(HomeModel model,
            DateTime proposalDate, string authToken)
        {
            var (success, proposals) = await _apiProvider
                .GetProposalsAsync(proposalDate, authToken)
                .ConfigureAwait(false);

            if (success)
            {
                foreach (var p in proposals)
                {
                    model.SetPropertiesFromProposal(p, GetCountries(), GetPositions());
                }
            }
        }
    }
}
