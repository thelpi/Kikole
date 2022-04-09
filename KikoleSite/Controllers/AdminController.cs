using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers
{
    public class AdminController : KikoleBaseController
    {
        private readonly IStringLocalizer<AdminController> _localizer;

        public AdminController(IApiProvider apiProvider, IStringLocalizer<AdminController> localizer)
            : base(apiProvider)
        {
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> PlayerSubmission()
        {
            var (token, _) = GetAuthenticationCookie();
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
                            ClueEditLanguages = new Dictionary<Languages, string>
                            {
                                { Languages.fr, model.ClueOverwriteFr }
                            },
                            ClueEditEn = model.ClueOverwriteEn,
                            EasyClueEditLanguages = new Dictionary<Languages, string>
                            {
                                { Languages.fr, model.EasyClueOverwriteFr }
                            },
                            EasyClueEditEn = model.EasyClueOverwriteEn,
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
                    model.ErrorMessage = _localizer["InvalidSelectedPlayer"];
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

            var chart = await _apiProvider
                .GetProposalChartAsync()
                .ConfigureAwait(false);

            var model = new PlayerCreationModel();
            SetPositionsOnModel(model, chart);
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

            var chart = await _apiProvider
                .GetProposalChartAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.ErrorMessage = _localizer["MandatName"];
                SetPositionsOnModel(model, chart);
                return View(model);
            }

            if (model.YearOfBirth == null || !ushort.TryParse(model.YearOfBirth, out var yearValue))
            {
                model.ErrorMessage = _localizer["InvalidYear"];
                SetPositionsOnModel(model, chart);
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.ClueEn))
            {
                model.ErrorMessage = _localizer["MandatClue"];
                SetPositionsOnModel(model, chart);
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.EasyClueEn))
            {
                model.ErrorMessage = _localizer["MandatClue"];
                SetPositionsOnModel(model, chart);
                return View(model);
            }

            var countries = await _apiProvider
                .GetCountriesAsync()
                .ConfigureAwait(false);

            if (model.Country == null
                || !ulong.TryParse(model.Country, out var countryId)
                || !countries.Any(c => countryId == c.Key))
            {
                model.ErrorMessage = _localizer["InvalidCountry"];
                SetPositionsOnModel(model, chart);
                return View(model);
            }

            if (model.Position == null
                || !ulong.TryParse(model.Position, out var positionId)
                || !GetPositions().Any(p => p.Key == positionId))
            {
                model.ErrorMessage = _localizer["InvalidPosition"];
                SetPositionsOnModel(model, chart);
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

            var clubsReferential = await _apiProvider
                .GetClubsAsync()
                .ConfigureAwait(false);

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
                model.ErrorMessage = _localizer["OneClubMin"];
                SetPositionsOnModel(model, chart);
                return View(model);
            }
            else if (clubs.Count != clubs.Distinct().Count())
            {
                model.ErrorMessage = _localizer["DuplicateClub"];
                SetPositionsOnModel(model, chart);
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
                ClueEn = model.ClueEn,
                EasyClueEn = model.EasyClueEn,
                ClueLanguages = new Dictionary<Languages, string>
                {
                    { Languages.fr, model.ClueFr }
                },
                EasyClueLanguages = new Dictionary<Languages, string>
                {
                    { Languages.fr, model.EasyClueFr }
                },
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
                model.ErrorMessage = _localizer["CreatingError", response];
                SetPositionsOnModel(model, chart);
                return View(model);
            }

            model = new PlayerCreationModel
            {
                InfoMessage = _localizer["PlayerOk"]
            };
            SetPositionsOnModel(model, chart);
            model.DisplayPlayerSubmissionLink = await _apiProvider
                .IsAdminUserAsync(token)
                .ConfigureAwait(false);
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
                model.ErrorMessage = _localizer["ClubNameMiss"];
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
                model.ErrorMessage = _localizer["CreatingError", response];
                return View("Club", model);
            }

            // force cache reset
            await _apiProvider.GetClubsAsync(true).ConfigureAwait(false);

            model = new ClubCreationModel
            {
                InfoMessage = _localizer["ClubOk"]
            };
            return View("Club", model);
        }

        private async Task<List<PlayerSubmissionModel>> GetPlayerSubmissionsList(string token)
        {
            var pls = await _apiProvider
                .GetPlayerSubmissionsAsync(token)
                .ConfigureAwait(false);

            var countries = await _apiProvider
                .GetCountriesAsync()
                .ConfigureAwait(false);

            return pls
                .Select(p => new PlayerSubmissionModel
                {
                    AllowedNames = string.Join(';', p.AllowedNames),
                    Clubs = p.Clubs,
                    Clue = p.Clue,
                    EasyClue = p.EasyClue,
                    Country = countries[p.Country],
                    Id = p.Id,
                    Login = p.Login,
                    Name = p.Name,
                    Position = Enum.GetValues(typeof(Position)).Cast<Position>().First(pp => (ulong)pp == p.Position).ToString(),
                    YearOfBirth = p.YearOfBirth
                })
                .ToList();
        }

        private void AddClubIfValid(List<ulong> clubs, string value, IReadOnlyCollection<Club> clubsReferential)
        {
            ulong? id = clubsReferential.FirstOrDefault(c => value == c.Name)?.Id;
            if (id.HasValue)
                clubs.Add(id.Value);
        }

        private void SetPositionsOnModel(PlayerCreationModel model, Api.ProposalChart chart)
        {
            model.Positions = new[] { new SelectListItem("", "0") }
                .Concat(GetPositions()
                    .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
                .ToList();
            model.Chart = chart;
        }
    }
}
