using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Cookies;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class AdminController : KikoleBaseController
    {
        private readonly IApiProvider _apiProvider;

        private static (IReadOnlyCollection<Club> clubs, DateTime expiration) _clubsCache;

        public AdminController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
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
    }
}
