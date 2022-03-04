using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiProvider _apiProvider;

        public HomeController(ApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        public IActionResult Index()
        {
            var model = new MainModel
            {
                Countries = Enum.GetValues(typeof(Country)).Cast<Country>().ToDictionary(c => c, c => c.ToString())
            };

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
            model.Countries = Enum.GetValues(typeof(Country)).Cast<Country>().ToDictionary(c => c, c => c.ToString());

            if (response.Successful)
            {
                switch (proposalType)
                {
                    case ProposalType.Club:
                    case ProposalType.Clue:
                        var clubSubmissions = model.ClubsOkSubmitted?.Split(';')?.ToList() ?? new List<string>();
                        clubSubmissions.Add(response.Value);
                        model.ClubsOkSubmitted = string.Join(';', clubSubmissions.Distinct().ToList());
                        break;
                    case ProposalType.Country:
                        model.CountryOkSubmitted = response.Value;
                        break;
                    case ProposalType.Name:
                        model.NameOkSubmitted = response.Value;
                        break;
                    case ProposalType.Year:
                        model.YearOkSubmitted = response.Value;
                        break;
                }
            }

            return View(model);
        }
    }
}
