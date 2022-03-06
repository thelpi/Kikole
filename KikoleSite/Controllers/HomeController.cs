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
        const string CookieName = "SubmissionForm";

        const int BasePoints = 1000;
        const int CluePointsRemoval = 500;
        const int NamePointsRemoval = 200;
        const int DefaultPointsRemoval = 100;

        private readonly ApiProvider _apiProvider;

        public HomeController(ApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        public IActionResult Index()
        {
            var model = GetSubmissionFormCookie()?.ClearNonPersistentData()
                ?? new MainModel
                {
                    Points = BasePoints
                };

            SetSubmissionFormCookie(model);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(MainModel model)
        {
            var proposalType = Enum.Parse<ProposalType>(
                HttpContext.Request.Form.Keys.Single(x => x.StartsWith("submit-")).Split('-')[1]);

            string value = null;
            switch (proposalType)
            {
                case ProposalType.Club: value = model.ClubNameSubmission; break;
                case ProposalType.Country: value = model.CountryNameSubmission; break;
                case ProposalType.Name: value = model.PlayerNameSubmission; break;
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

            model = (GetSubmissionFormCookie() ?? model).ClearNonPersistentData();

            model.IsErrorMessage = !response.Successful;
            model.MessageToDisplay = proposalType == ProposalType.Clue
                ? "A clue has been given in career clubs section"
                : (response.Successful
                    ? $"Valid {proposalType} guess"
                    : $"Invalid {proposalType} guess");

            if (response.Successful)
            {
                switch (proposalType)
                {
                    case ProposalType.Club:
                    case ProposalType.Clue:
                        if (proposalType == ProposalType.Clue)
                        {
                            model.RemovePoints(CluePointsRemoval);
                        }
                        var clubSubmissions = model.KnownPlayerClubs?.ToList() ?? new List<PlayerClub>();
                        if (!clubSubmissions.Any(cs => cs.Name == response.Value.name.ToString()))
                        {
                            clubSubmissions.Add(new PlayerClub
                            {
                                HistoryPosition = response.Value.historyPosition,
                                ImportancePosition = response.Value.importancePosition,
                                Name = response.Value.name.ToString()
                            });
                        }
                        model.KnownPlayerClubs = clubSubmissions.OrderBy(cs => cs.HistoryPosition).ToList();
                        break;
                    case ProposalType.Country:
                        model.CountryName = Constants.Countries.First(c => c.Key.ToString() == response.Value.ToString()).Value;
                        break;
                    case ProposalType.Name:
                        model.PlayerName = response.Value.ToString();
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
                }
            }

            SetSubmissionFormCookie(model);

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

        private MainModel GetSubmissionFormCookie()
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

        private void SetSubmissionFormCookie(MainModel model)
        {
            SetCookie(CookieName, JsonConvert.SerializeObject(model), DateTime.Now.AddDays(1).Date);
        }
    }
}
