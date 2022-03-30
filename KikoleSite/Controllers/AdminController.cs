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
        public AdminController(IApiProvider apiProvider)
            : base(apiProvider)
        { }

        [HttpGet]
        public async Task<IActionResult> PlayerSubmission()
        {
            var (token, _) = this.GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("Index", "Home");
            }

            var players = await GetPlayerSubmissionsList(token)
                .ConfigureAwait(false);

            var model = new PlayerSubmissionsModel
            {
                Players = players
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PlayerSubmission(PlayerSubmissionsModel model)
        {
            var (token, _) = this.GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("Index", "Home");
            }

            if (model == null)
            {
                return RedirectToAction("PlayerSubmission", "Admin");
            }

            model.Players = await GetPlayerSubmissionsList(token)
                .ConfigureAwait(false);

            if (model.Players.Count == 0)
            {
                return RedirectToAction("PlayerSubmission", "Admin");
            }

            var action = GetSubmitAction();

            if (action == "accepted" || action == "refusal")
            {
                var result = await _apiProvider
                    .ValidatePlayerSubmissionAsync(
                        new PlayerSubmissionValidationRequest
                        {
                            ClueEdit = model.ClueOverwrite,
                            IsAccepted = action == "accepted",
                            PlayerId = model.SelectedId,
                            RefusalReason = model.RefusalReason
                        },
                        token)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    model.ErrorMessage = result;
                }
                else
                {
                    return RedirectToAction("PlayerSubmission", "Admin");
                }
            }
            else if (action == "pchoice")
            {
                if (model.SelectedPlayer == null)
                {
                    model.ErrorMessage = "Invalid selected player";
                    model.SelectedId = 0;
                }
            }
            else
            {
                return RedirectToAction("PlayerSubmission", "Admin");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (token, _) = this.GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsPowerUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new PlayerCreationModel();
            SetPositionsOnModel(model);
            model.DisplayPlayerSubmissionLink = await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(PlayerCreationModel model)
        {
            var (token, _) = this.GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsPowerUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("Index", "Home");
            }

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

            var countries = await _apiProvider
                .GetCountriesAsync(DefaultLanguageId)
                .ConfigureAwait(false);

            if (model.Country == null
                || !ulong.TryParse(model.Country, out var countryId)
                || !countries.Any(c => countryId == c.Key))
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
            
            var names = new List<string>
            {
                model.AlternativeName0, model.AlternativeName1,
                model.AlternativeName2, model.AlternativeName3,
                model.AlternativeName4, model.AlternativeName5,
                model.AlternativeName6, model.AlternativeName7,
                model.AlternativeName8, model.AlternativeName9,
            };
            names = names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();

            var clubsReferential = await _apiProvider.GetClubsAsync().ConfigureAwait(false);

            var clubs = new List<ulong>();
            AddClubIfValid(clubs, model.Club0, clubsReferential);
            AddClubIfValid(clubs, model.Club1, clubsReferential);
            AddClubIfValid(clubs, model.Club2, clubsReferential);
            AddClubIfValid(clubs, model.Club3, clubsReferential);
            AddClubIfValid(clubs, model.Club4, clubsReferential);
            AddClubIfValid(clubs, model.Club5, clubsReferential);
            AddClubIfValid(clubs, model.Club6, clubsReferential);
            AddClubIfValid(clubs, model.Club7, clubsReferential);
            AddClubIfValid(clubs, model.Club8, clubsReferential);
            AddClubIfValid(clubs, model.Club9, clubsReferential);

            if (clubs.Count == 0)
            {
                model.ErrorMessage = "One club is mandatory";
                SetPositionsOnModel(model);
                return View(model);
            }

            var isAdmin = await _apiProvider
                .IsAdminUserAsync(token)
                .ConfigureAwait(false);

            var req = new PlayerRequest
            {
                SetLatestProposalDate = isAdmin,
                AllowedNames = names,
                Clubs = clubs,
                Clue = model.Clue,
                Country = countryId.ToString(),
                Name = model.Name,
                Position = positionId.ToString(),
                YearOfBirth = yearValue,
                HideCreator = model.HideCreator
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
            model.DisplayPlayerSubmissionLink = await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Club()
        {
            var (token, _) = this.GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsPowerUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("Index", "Home");
            }

            return View("Club", new ClubCreationModel());
        }

        [HttpPost]
        public async Task<IActionResult> Club(ClubCreationModel model)
        {
            var (token, _) = this.GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsPowerUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(model.MainName))
            {
                model.ErrorMessage = "Main name is mandatory";
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
            await _apiProvider.GetClubsAsync(true).ConfigureAwait(false);

            model = new ClubCreationModel
            {
                InfoMessage = "Club created!"
            };
            return View("Club", model);
        }

        private async Task<List<PlayerSubmissionModel>> GetPlayerSubmissionsList(string token)
        {
            var pls = await _apiProvider
                            .GetPlayerSubmissionsAsync(token)
                            .ConfigureAwait(false);

            var countries = await _apiProvider
                .GetCountriesAsync(DefaultLanguageId)
                .ConfigureAwait(false);

            var players = pls
                .Select(p => new PlayerSubmissionModel
                {
                    AllowedNames = string.Join(';', p.AllowedNames),
                    Clubs = p.Clubs,
                    Clue = p.Clue,
                    Country = countries[p.Country],
                    Id = p.Id,
                    Login = p.Login,
                    Name = p.Name,
                    Position = Enum.GetValues(typeof(Position)).Cast<Position>().First(pp => (ulong)pp == p.Position).ToString(),
                    YearOfBirth = p.YearOfBirth
                })
                .ToList();
            return players;
        }

        private void AddClubIfValid(List<ulong> clubs, string value, IReadOnlyCollection<Club> clubsReferential)
        {
            ulong? id = clubsReferential.FirstOrDefault(c => value == c.Name)?.Id;
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
    }
}
