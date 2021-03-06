using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;
using KikoleSite.Api.Models.Requests;
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
        public async Task<IActionResult> Actions()
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("ErrorIndex", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Actions(object dummy)
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("ErrorIndex", "Home");
            }

            var action = GetSubmitAction();

            switch (action)
            {
                case "recomputebadges":
                    await _apiProvider
                        .ResetBadgesAsync()
                        .ConfigureAwait(false);
                    break;
                case "recomputeleaders":
                    await _apiProvider
                        .ComputeMissingLeadersAsync()
                        .ConfigureAwait(false);
                    break;
                case "reassignplayers":
                    await _apiProvider
                        .ReassignPlayersOfTheDayAsync()
                        .ConfigureAwait(false);
                    break;
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PlayerSubmission()
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("ErrorIndex", "Home");
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
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("ErrorIndex", "Home");
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
        public async Task<IActionResult> Index(bool withOkMessage)
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsPowerUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("ErrorIndex", "Home");
            }

            var chart = await _apiProvider
                .GetProposalChartAsync()
                .ConfigureAwait(false);

            var model = new PlayerCreationModel();
            if (withOkMessage)
                model.InfoMessage = _localizer["PlayerOk"];
            SetPositionsOnModel(model, chart);
            model.DisplayPlayerSubmissionLink = await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(PlayerCreationModel model)
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsPowerUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("ErrorIndex", "Home");
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

            byte iPos = 1;
            var clubs = new List<PlayerClubRequest>();
            AddClubIfValid(clubs, model.Club0, clubsReferential, ref iPos, model.IsLoan0);
            AddClubIfValid(clubs, model.Club1, clubsReferential, ref iPos, model.IsLoan1);
            AddClubIfValid(clubs, model.Club2, clubsReferential, ref iPos, model.IsLoan2);
            AddClubIfValid(clubs, model.Club3, clubsReferential, ref iPos, model.IsLoan3);
            AddClubIfValid(clubs, model.Club4, clubsReferential, ref iPos, model.IsLoan4);
            AddClubIfValid(clubs, model.Club5, clubsReferential, ref iPos, model.IsLoan5);
            AddClubIfValid(clubs, model.Club6, clubsReferential, ref iPos, model.IsLoan6);
            AddClubIfValid(clubs, model.Club7, clubsReferential, ref iPos, model.IsLoan7);
            AddClubIfValid(clubs, model.Club8, clubsReferential, ref iPos, model.IsLoan8);
            AddClubIfValid(clubs, model.Club9, clubsReferential, ref iPos, model.IsLoan9);
            AddClubIfValid(clubs, model.Club10, clubsReferential, ref iPos, model.IsLoan10);
            AddClubIfValid(clubs, model.Club11, clubsReferential, ref iPos, model.IsLoan11);
            AddClubIfValid(clubs, model.Club12, clubsReferential, ref iPos, model.IsLoan12);
            AddClubIfValid(clubs, model.Club13, clubsReferential, ref iPos, model.IsLoan13);
            AddClubIfValid(clubs, model.Club14, clubsReferential, ref iPos, model.IsLoan14);

            if (clubs.Count == 0)
            {
                model.ErrorMessage = _localizer["OneClubMin"];
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
                Country = (Countries)countryId,
                Name = model.Name,
                Position = (Positions)positionId,
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

            return RedirectToAction("Index", "Admin", new { withOkMessage = true });
        }

        [HttpGet]
        public async Task<IActionResult> Club()
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsPowerUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("ErrorIndex", "Home");
            }

            return View("Club", new ClubCreationModel());
        }

        [HttpPost]
        public async Task<IActionResult> Club(ClubCreationModel model)
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsPowerUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("ErrorIndex", "Home");
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

        [HttpGet]
        public async Task<IActionResult> PlayerEdit(ulong playerId)
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false))
                || playerId == 0)
            {
                return RedirectToAction("ErrorIndex", "Home");
            }

            var (clueEn, clueFr, easyClueEn, easyClueFr, error) = await _apiProvider
                .GetPlayerCluesAsync(playerId, token)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(error))
            {
                return RedirectToAction("ErrorIndex", "Home");
            }

            var model = new PlayerEditModel
            {
                PlayerId = playerId,
                ClueEn = clueEn,
                ClueFr = clueFr,
                EasyClueEn = easyClueEn,
                EasyClueFr = easyClueFr
            };

            return View("PlayerEdit", model);
        }

        [HttpPost]
        public async Task<IActionResult> PlayerEdit(PlayerEditModel model)
        {
            var (token, _) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token)
                || !(await _apiProvider.IsAdminUserAsync(token).ConfigureAwait(false)))
            {
                return RedirectToAction("ErrorIndex", "Home");
            }

            if (string.IsNullOrWhiteSpace(model.ClueEn)
                || string.IsNullOrWhiteSpace(model.ClueFr)
                || string.IsNullOrWhiteSpace(model.EasyClueEn)
                || string.IsNullOrWhiteSpace(model.EasyClueFr)
                || model.PlayerId == 0)
            {
                model.Message = "Données de formulaire invalides";
                return View("PlayerEdit", model);
            }

            var result = await _apiProvider
                .UpdatePlayerCluesAsync(model.PlayerId, model.ClueEn, model.EasyClueEn, model.ClueFr, model.EasyClueFr, token)
                .ConfigureAwait(false);

            model.Success = string.IsNullOrWhiteSpace(result);
            model.Message = result;

            return View("PlayerEdit", model);
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
                    Country = countries[(ulong)p.Country],
                    Id = p.Id,
                    Login = p.Login,
                    Name = p.Name,
                    Position = Enum.GetValues(typeof(Positions)).Cast<Positions>().First(pp => pp == p.Position).ToString(),
                    YearOfBirth = p.YearOfBirth
                })
                .ToList();
        }

        private void AddClubIfValid(List<PlayerClubRequest> clubs, string value, IReadOnlyCollection<Club> clubsReferential, ref byte i, bool isLoan)
        {
            ulong? id = clubsReferential.FirstOrDefault(c => value == c.Name)?.Id;
            if (id.HasValue)
            {
                clubs.Add(new PlayerClubRequest { ClubId = id.Value, HistoryPosition = i, IsLoan = isLoan });
                i++;
            }
        }

        private void SetPositionsOnModel(PlayerCreationModel model, ProposalChart chart)
        {
            model.Positions = new[] { new SelectListItem("", "0") }
                .Concat(GetPositions()
                    .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
                .ToList();
            model.Chart = chart;
        }
    }
}
