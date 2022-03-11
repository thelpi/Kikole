using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Cookies;
using KikoleSite.ItemDatas;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class HomeController : KikoleBaseController
    {
        const ulong DefaultLanguageId = 1;

        const int BasePoints = 1000;

        static readonly DateTime FirstDate = new DateTime(2022, 3, 5);

        private readonly IApiProvider _apiProvider;

        private static readonly Dictionary<ulong, IReadOnlyDictionary<ulong, string>> _countriesCache
             = new Dictionary<ulong, IReadOnlyDictionary<ulong, string>>();

        public HomeController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        public IActionResult Index([FromQuery] int? day)
        {
            var model = GetCookieModelOrDefault(new HomeModel { Points = BasePoints });

            if (day.HasValue
                && model.CurrentDay != day.Value
                && day.Value >= 0
                && DateTime.Now.AddDays(-day.Value).Date >= FirstDate)
            {
                model = new HomeModel
                {
                    Points = BasePoints,
                    CurrentDay = day.Value,
                    NoPreviousDay = DateTime.Now.AddDays(-day.Value).Date == FirstDate
                };
            }

            this.SetSubmissionFormCookie(model.ToSubmissionFormCookie());

            model.LoggedAs = this.GetAuthenticationCookie().login;
            model.Countries = GetCountries();
            model.Positions = GetPositions();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(HomeModel model)
        {
            var proposalType = Enum.Parse<ProposalType>(GetSubmitAction());

            var value = model.GetValueFromProposalType(proposalType);

            var (token, login) = this.GetAuthenticationCookie();

            model = GetCookieModelOrDefault(model);

            var response = await _apiProvider
                .SubmitProposalAsync(DateTime.Now, value, model.CurrentDay,
                    proposalType,
                    token,
                    model.Points)
                .ConfigureAwait(false);

            model.Points = response.TotalPoints;
            model.IsErrorMessage = !response.Successful;
            model.MessageToDisplay = proposalType == ProposalType.Clue
                ? "A clue has been given, see below"
                : (response.Successful
                    ? $"Valid {proposalType} guess"
                    : $"Invalid {proposalType} guess{(!string.IsNullOrWhiteSpace(response.Tip) ? $"; {response.Tip}" : "")}");

            if (response.Successful)
            {
                switch (proposalType)
                {
                    case ProposalType.Club:
                        var clubSubmissions = model.KnownPlayerClubs?.ToList() ?? new List<PlayerClub>();
                        if (!clubSubmissions.Any(cs => cs.Name == response.Value.name.ToString()))
                        {
                            clubSubmissions.Add(new PlayerClub
                            {
                                HistoryPosition = response.Value.historyPosition,
                                Name = response.Value.name.ToString()
                            });
                        }
                        model.KnownPlayerClubs = clubSubmissions.OrderBy(cs => cs.HistoryPosition).ToList();
                        break;
                    case ProposalType.Country:
                        model.CountryName = GetCountries()[ulong.Parse(response.Value.ToString())];
                        break;
                    case ProposalType.Position:
                        model.Position = GetPositions()[ulong.Parse(response.Value.ToString())];
                        break;
                    case ProposalType.Name:
                        model.PlayerName = response.Value.ToString();
                        break;
                    case ProposalType.Year:
                        model.BirthYear = response.Value.ToString();
                        break;
                    case ProposalType.Clue:
                        model.Clue = response.Value.ToString();
                        break;
                }
            }

            this.SetSubmissionFormCookie(model.ToSubmissionFormCookie());

            model.LoggedAs = login;
            model.Countries = GetCountries();
            model.Positions = GetPositions();
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
    }
}
