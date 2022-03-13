using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Cookies;
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

        public HomeController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        public async Task<IActionResult> Index([FromQuery] int? day)
        {
            var (token, login) = this.GetAuthenticationCookie();

            var chart = GetProposalChartCache();

            var model = GetCookieModelOrDefault(new HomeModel { Points = chart.BasePoints });
            
            if (day.HasValue
                && model.CurrentDay != day.Value
                && day.Value >= 0
                && DateTime.Now.Date.AddDays(-day.Value) >= chart.FirstDate)
            {
                model = new HomeModel
                {
                    Points = chart.BasePoints,
                    CurrentDay = day.Value,
                    NoPreviousDay = DateTime.Now.Date.AddDays(-day.Value) == chart.FirstDate
                };
            }

            var proposalDate = DateTime.Now.Date.AddDays(-model.CurrentDay);

            await SetModelFromApiAsync(model, proposalDate, token).ConfigureAwait(false);

            this.SetSubmissionFormCookie(model.ToSubmissionFormCookie());
            
            return ViewWithFullModel(model, login);
        }

        [HttpPost]
        public async Task<IActionResult> Index(HomeModel model)
        {
            if (model == null || !Enum.TryParse<ProposalType>(GetSubmitAction(), out var proposalType))
            {
                return Redirect("/");
            }

            var value = model.GetValueFromProposalType(proposalType);

            var (token, login) = this.GetAuthenticationCookie();

            model = GetCookieModelOrDefault(model);

            var response = await _apiProvider
                .SubmitProposalAsync(DateTime.Now, value, model.CurrentDay,
                    proposalType,
                    token,
                    model.Points,
                    this.GetIp())
                .ConfigureAwait(false);

            model.IsErrorMessage = !response.Successful;
            model.MessageToDisplay = proposalType == ProposalType.Clue
                ? "A clue has been given, see below"
                : (response.Successful
                    ? $"Valid {proposalType} guess"
                    : $"Invalid {proposalType} guess{(!string.IsNullOrWhiteSpace(response.Tip) ? $"; {response.Tip}" : "")}");

            model.SetPropertiesFromProposal(response, GetCountries(), GetPositions());

            this.SetSubmissionFormCookie(model.ToSubmissionFormCookie());

            return ViewWithFullModel(model, login);
        }

        [HttpPost]
        public JsonResult AutoComplete(string prefix, ulong languageId = DefaultLanguageId)
        {
            var countries = GetCountries()
                .Where(c =>
                    c.Value.ToLowerInvariant().Contains(prefix.ToLowerInvariant()));

            return Json(countries);
        }

        private IActionResult ViewWithFullModel(HomeModel model, string login)
        {
            model.LoggedAs = login;
            model.Positions = new[] { new SelectListItem("", "0") }
                .Concat(GetPositions()
                    .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
                .ToList();
            return View(model);
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

        private HomeModel GetCookieModelOrDefault(HomeModel defaultModel)
        {
            var cookieSubForm = this.GetSubmissionFormCookie();

            return cookieSubForm != null
                ? new HomeModel(cookieSubForm)
                : defaultModel;
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
                .GetProposalsAsync(proposalDate, authToken, this.GetIp())
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
