using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.ItemDatas;
using KikoleSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace KikoleSite.Controllers
{
    public class HomeController : Controller
    {
        const string CookieName = "SubmissionForm";

        const ulong DefaultLanguageId = 1;

        const int BasePoints = 1000;
        const int NamePointsRemoval = 200;
        const int DefaultPointsRemoval = 100;
        const int YearPointsRemoval = 50;

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
            var model = GetSubmissionFormCookie()?.ClearNonPersistentData()
                ?? new HomeModel
                {
                    Points = BasePoints
                };

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

            SetSubmissionFormCookie(model);

            model.LoggedAs = GetCookieAuthValue().Item2;
            model.Countries = GetCountries();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(HomeModel model)
        {
            var proposalType = Enum.Parse<ProposalType>(
                HttpContext.Request.Form.Keys.Single(x => x.StartsWith("submit-")).Split('-')[1]);

            string value = null;
            switch (proposalType)
            {
                case ProposalType.Club: value = model.ClubNameSubmission; break;
                case ProposalType.Country: value = model.CountryNameSubmission; break;
                case ProposalType.Name: value = model.PlayerNameSubmission; break;
                case ProposalType.Year: value = model.BirthYearSubmission; break;
            }

            var cookieAuth = GetCookieAuthValue();

            model = (GetSubmissionFormCookie() ?? model).ClearNonPersistentData();

            var response = await _apiProvider
                .SubmitProposalAsync(DateTime.Now, value, model.CurrentDay,
                    proposalType,
                    cookieAuth.Item1)
                .ConfigureAwait(false);

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
                    case ProposalType.Name:
                        model.PlayerName = response.Value.ToString();
                        break;
                    case ProposalType.Year:
                        model.BirthYear = response.Value.ToString();
                        break;
                    case ProposalType.Clue:
                        model.RemovePoints(DefaultPointsRemoval);
                        model.Clue = response.Value.ToString();
                        break;
                }
            }
            else
            {
                switch (proposalType)
                {
                    case ProposalType.Club:
                    case ProposalType.Country:
                        model.RemovePoints(DefaultPointsRemoval);
                        break;
                    case ProposalType.Name:
                        model.RemovePoints(NamePointsRemoval);
                        break;
                    case ProposalType.Year:
                        model.RemovePoints(YearPointsRemoval);
                        break;
                }
            }

            SetSubmissionFormCookie(model);

            model.LoggedAs = cookieAuth.Item2;
            model.Countries = GetCountries();
            return View(model);
        }

        private void SetCookie(string cookieName, string cookieValue, DateTime expiration)
        {
            Response.Cookies.Delete(cookieName);
            var option = new CookieOptions
            {
                Expires = expiration,
                IsEssential = true,
                Secure = false
            };
            Response.Cookies.Append(cookieName, cookieValue, option);
        }

        private (string, string) GetCookieAuthValue()
        {
            if (Request.Cookies.TryGetValue("AccountForm", out string cookieValue))
            {
                var cookieParts = cookieValue.Split("§§§");
                if (cookieParts.Length > 1)
                {
                    return (cookieParts[0], cookieParts[1]);
                }
            }

            return (null, null);
        }

        private HomeModel GetSubmissionFormCookie()
        {
            if (Request.Cookies.TryGetValue(CookieName, out string cookieValue))
            {
                try
                {
                    return JsonConvert.DeserializeObject<HomeModel>(cookieValue);
                }
                catch { }
            }
            return null;
        }

        private void SetSubmissionFormCookie(HomeModel model)
        {
            SetCookie(CookieName, JsonConvert.SerializeObject(model), DateTime.Now.AddDays(1).Date);
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
    }
}
