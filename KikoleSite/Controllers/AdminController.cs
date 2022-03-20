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
    public class AdminController : KikoleBaseController
    {
        const ulong DefaultLanguageId = 1;

        private readonly IApiProvider _apiProvider;

        private static (IReadOnlyCollection<Club> clubs, DateTime expiration) _clubsCache;
        private static readonly Dictionary<ulong, IReadOnlyDictionary<ulong, string>> _countriesCache
             = new Dictionary<ulong, IReadOnlyDictionary<ulong, string>>();

        public AdminController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new PlayerCreationModel();
            SetPositionsOnModel(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(PlayerCreationModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.ErrorMessage = "Name is mandatory";
                SetPositionsOnModel(model);
                return View(model);
            }

            if (model.YearOfBirth == null || !ushort.TryParse(model.YearOfBirth, out var yearValue))
            {
                model.ErrorMessage = "Year is invalid";
                SetPositionsOnModel(model);
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Clue))
            {
                model.ErrorMessage = "Clue is mandatory";
                SetPositionsOnModel(model);
                return View(model);
            }

            if (model.Country == null
                || !ulong.TryParse(model.Country, out var countryId)
                || !GetCountries().Any(c => countryId == c.Key))
            {
                model.ErrorMessage = "Country is mandatory";
                SetPositionsOnModel(model);
                return View(model);
            }

            if (model.Position == null
                || !ulong.TryParse(model.Position, out var positionId)
                || !GetPositions().Any(p => p.Key == positionId))
            {
                model.ErrorMessage = "Position is mandatory";
                SetPositionsOnModel(model);
                return View(model);
            }
            
            var (token, _) = this.GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
            {
                model.ErrorMessage = "You must be authenticated to do this action";
                SetPositionsOnModel(model);
                return View(model);
            }

            var names = new List<string>
            {
                model.AlternativeName0, model.AlternativeName1,
                model.AlternativeName2, model.AlternativeName3,
                model.AlternativeName4, model.AlternativeName5,
                model.AlternativeName6, model.AlternativeName7,
                model.AlternativeName8, model.AlternativeName9,
            };
            names = names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();

            var clubs = new List<ulong>();
            AddClubIfValid(clubs, model.Club0);
            AddClubIfValid(clubs, model.Club1);
            AddClubIfValid(clubs, model.Club2);
            AddClubIfValid(clubs, model.Club3);
            AddClubIfValid(clubs, model.Club4);
            AddClubIfValid(clubs, model.Club5);
            AddClubIfValid(clubs, model.Club6);
            AddClubIfValid(clubs, model.Club7);
            AddClubIfValid(clubs, model.Club8);
            AddClubIfValid(clubs, model.Club9);

            if (clubs.Count == 0)
            {
                model.ErrorMessage = "One club is mandatory";
                SetPositionsOnModel(model);
                return View(model);
            }

            var req = new PlayerRequest
            {
                SetLatestProposalDate = true,
                AllowedNames = names,
                Clubs = clubs,
                Clue = model.Clue,
                Country = countryId.ToString(),
                Name = model.Name,
                Position = positionId.ToString(),
                YearOfBirth = yearValue
            };

            var response = await _apiProvider
                .CreatePlayerAsync(req, token)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(response))
            {
                model.ErrorMessage = $"Error while creating: {response}";
                SetPositionsOnModel(model);
                return View(model);
            }

            model = new PlayerCreationModel
            {
                InfoMessage = "Player created!"
            };
            SetPositionsOnModel(model);
            return View(model);
        }

        private void AddClubIfValid(List<ulong> clubs, string value)
        {
            ulong? id = GetClubs().FirstOrDefault(c => value == c.Name)?.Id;
            if (id.HasValue)
                clubs.Add(id.Value);
        }

        private void SetPositionsOnModel(PlayerCreationModel model)
        {
            model.Positions = new[] { new SelectListItem("", "0") }
                .Concat(GetPositions()
                    .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
                .ToList();
        }

        [HttpGet]
        public IActionResult Club()
        {
            return View("Club", new ClubCreationModel());
        }

        [HttpPost]
        public async Task<IActionResult> Club(ClubCreationModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MainName))
            {
                model.ErrorMessage = "Main name is mandatory";
                return View("Club", model);
            }

            var (token, _) = this.GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
            {
                model.ErrorMessage = "You must be authenticated to do this action";
                return View("Club", model);
            }

            var names = new[]
            {
                model.AlternativeName0, model.AlternativeName1,
                model.AlternativeName2, model.AlternativeName3,
                model.AlternativeName4, model.AlternativeName5,
                model.AlternativeName6, model.AlternativeName7,
                model.AlternativeName8, model.AlternativeName9
            };

            names = names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToArray();

            var response = await _apiProvider
                .CreateClubAsync(model.MainName, names, token)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(response))
            {
                model.ErrorMessage = $"Error while creating: {response}";
                return View("Club", model);
            }

            // force cache reset
            _clubsCache = (null, DateTime.Now);
            GetClubs();

            model = new ClubCreationModel
            {
                InfoMessage = "Club created!"
            };
            return View("Club", model);
        }

        [HttpPost]
        public JsonResult AutoCompleteClubs(string prefix)
        {
            var clubs = GetClubs()
                .Where(c =>
                    c.Name.ToLowerInvariant().Contains(prefix.ToLowerInvariant()));

            return Json(clubs.Select(x => x.Name));
        }

        [HttpPost]
        public JsonResult AutoCompleteCountries(string prefix, ulong languageId = DefaultLanguageId)
        {
            var countries = GetCountries()
                .Where(c =>
                    c.Value.ToLowerInvariant().Contains(prefix.ToLowerInvariant()));

            return Json(countries);
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

        private IReadOnlyDictionary<ulong, string> GetPositions()
        {
            return Enum
                .GetValues(typeof(Position))
                .Cast<Position>()
                .ToDictionary(_ => (ulong)_, _ => _.ToString());
        }
    }
}
