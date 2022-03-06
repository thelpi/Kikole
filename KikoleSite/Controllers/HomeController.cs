using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace KikoleSite.Controllers
{
    public class HomeController : Controller
    {
        private const string CookieName = "KikoleKooKie";

        private readonly ApiProvider _apiProvider;

        public HomeController(ApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        public IActionResult Index()
        {
            var model = GetModelCookie() ?? new MainModel
            {
                Points = 1000
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(MainModel model)
        {
            model.ClubsOkSubmitted = model.ToClubsOkSubmitted();

            var proposalType = Enum.Parse<ProposalType>(
                HttpContext.Request.Form.Keys.Single(x => x.StartsWith("submit-")).Split('-')[1]);

            string value = null;
            switch (proposalType)
            {
                case ProposalType.Club: value = model.SelectedValueClub; break;
                case ProposalType.Country: value = model.SelectedValueCountry.ToString(); break;
                case ProposalType.Name: value = model.SelectedValueName; break;
                case ProposalType.Year: value = model.SelectedValueYear; break;
            }

            var response = await _apiProvider
                .SubmitProposalAsync(
                    new ProposalRequest
                    {
                        ProposalDate = DateTime.Now,
                        Value = value
                    },
                    proposalType)
                .ConfigureAwait(false);

            model.HasWrongGuess = !response.Successful;
            model.SelectedValueClub = null;
            model.SelectedValueCountry = Country.AF;
            model.SelectedValueName = null;
            model.SelectedValueYear = null;

            if (response.Successful)
            {
                switch (proposalType)
                {
                    case ProposalType.Club:
                    case ProposalType.Clue:
                        if (proposalType == ProposalType.Clue)
                        {
                            model.RemovePoints(500);
                        }
                        var clubSubmissions = model.ClubsOkSubmitted ?? new List<PlayerClub>();
                        if (!clubSubmissions.Any(cs => cs.Name == response.Value.name.ToString()))
                        {
                            clubSubmissions.Add(new PlayerClub
                            {
                                HistoryPosition = response.Value.historyPosition,
                                ImportancePosition = response.Value.importancePosition,
                                Name = response.Value.name.ToString()
                            });
                        }
                        model.ClubsOkSubmitted = clubSubmissions.OrderBy(cs => cs.HistoryPosition).ToList();
                        break;
                    case ProposalType.Country:
                        model.CountryOkSubmitted = Constants.Countries.First(c => c.Key.ToString() == response.Value.ToString()).Value;
                        break;
                    case ProposalType.Name:
                        model.NameOkSubmitted = response.Value.ToString();
                        break;
                    case ProposalType.Year:
                        model.YearOkSubmitted = response.Value.ToString();
                        break;
                }
            }
            else
            {
                switch (proposalType)
                {
                    case ProposalType.Club:
                    case ProposalType.Country:
                        model.RemovePoints(100);
                        break;
                    case ProposalType.Name:
                        model.RemovePoints(200);
                        break;
                    case ProposalType.Year:
                        model.RemovePoints(50);
                        break;
                }
            }

            SetModelCookie(model);

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

        private MainModel GetModelCookie()
        {
            if (Request.Cookies.TryGetValue(CookieName, out string cookieValue))
            {
                try
                {
                    return JsonConvert.DeserializeObject<MainModel>(cookieValue);
                }
                catch { }
            }
            return null;
        }

        private void SetModelCookie(MainModel model)
        {
            SetCookie(CookieName, JsonConvert.SerializeObject(model), DateTime.Now.AddDays(1).Date);
        }
    }
}
