using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Helpers;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers;

public class AdminController : KikoleBaseController
{
    private readonly IStringLocalizer<AdminController> _localizer;
    private readonly IDiscussionRepository _discussionRepository;
    private readonly ILeaderService _leaderService;
    private readonly IMessageRepository _messageRepository;

    public AdminController(IStringLocalizer<AdminController> localizer,
        IUserRepository userRepository,
        IInternationalService internationalService,
        IMessageRepository messageRepository,
        IClock clock,
        IGameCalendar gameCalendar,
        IPlayerService playerService,
        IBadgeService badgeService,
        ILeaderService leaderService,
        IDiscussionRepository discussionRepository,
        IHttpContextAccessor httpContextAccessor)
        : base(userRepository,
            internationalService,
            clock,
            gameCalendar,
            playerService,
            badgeService,
            httpContextAccessor)
    {
        _localizer = localizer;
        _discussionRepository = discussionRepository;
        _leaderService = leaderService;
        _messageRepository = messageRepository;
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Actions()
    {
        // default : from now without ms to tomorrow 23:59:59
        return View(new AdminModel
        {
            MessageDateStart = _clock.NowSeconds,
            MessageDateEnd = _clock.TomorrowEnd,
            Discussions = await _discussionRepository
                .GetDiscussionsAsync()
        });
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Actions(AdminModel model)
    {
        var action = GetSubmitAction();

        switch (action)
        {
            case "recomputebadges":
                await _badgeService
                    .ResetBadgesAsync(ViewHelper.GetLanguage());
                break;
            case "recomputeleaders":
                await _leaderService
                    .ComputeMissingLeadersAsync();
                break;
            case "reassignplayers":
                await _playerService
                    .ReassignPlayersOfTheDayAsync();
                break;
            case "insertmessage":
                if (model == null)
                    return RedirectToAction("ErrorIndex", "Home");

                await _messageRepository
                    .InsertMessageAsync(new Models.Dtos.MessageDto
                    {
                        DisplayTo = model.MessageDateEnd,
                        DisplayFrom = model.MessageDateStart,
                        CreationDate = _clock.Now,
                        Message = model.Message ?? string.Empty
                    });

                model.Message = null;
                model.ActionFeedback = "Annonce créée";
                break;
        }

        return View(model);
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> PlayerSubmission()
    {
        var players = await GetPlayerSubmissionsList();

        var model = new PlayerSubmissionsModel
        {
            Players = players
        };

        return View(model);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> PlayerSubmission(PlayerSubmissionsModel model)
    {
        if (model == null)
            return RedirectToAction("PlayerSubmission", "Admin");

        model.Players = await GetPlayerSubmissionsList();

        if (model.Players.Count == 0)
            return RedirectToAction("PlayerSubmission", "Admin");

        var action = GetSubmitAction();

        if (action == "accepted" || action == "refusal")
        {
            var request = new PlayerSubmissionValidationRequest
            {
                ClueEditLanguages = new Dictionary<Languages, string?>
                {
                    { Languages.fr, model.ClueOverwriteFr }
                },
                ClueEditEn = model.ClueOverwriteEn,
                EasyClueEditLanguages = new Dictionary<Languages, string?>
                {
                    { Languages.fr, model.EasyClueOverwriteFr }
                },
                EasyClueEditEn = model.EasyClueOverwriteEn,
                IsAccepted = action == "accepted",
                PlayerId = model.SelectedId,
                RefusalReason = model.RefusalReason
            };

            var validityCheck = request.IsValid(_localizer);
            if (!string.IsNullOrWhiteSpace(validityCheck))
                model.ErrorMessage = string.Format(_localizer["InvalidRequest"], validityCheck);
            else
            {
                var (result, userId, badges) = await _playerService
                    .ValidatePlayerSubmissionAsync(request);

                if (result == PlayerSubmissionErrors.PlayerNotFound)
                    model.ErrorMessage = _localizer["PlayerDoesNotExist"];
                else if (result == PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused)
                    model.ErrorMessage = _localizer["RejectAndPublicationDateCombined"];
                else
                {
                    foreach (var badge in badges)
                    {
                        await _badgeService
                            .AddBadgeToUserAsync(badge, userId);
                    }

                    return RedirectToAction("PlayerSubmission", "Admin");
                }
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
            return RedirectToAction("PlayerSubmission", "Admin");

        return View(model);
    }

    [HttpGet]
    [Authorization(UserTypes.PowerUser)]
    public IActionResult Index(bool withOkMessage)
    {
        var model = new PlayerCreationModel();
        if (withOkMessage)
            model.InfoMessage = _localizer["PlayerOk"];
        SetPositionsOnModel(model);
        model.DisplayPlayerSubmissionLink = IsTypeOfUser(UserTypes.Administrator);
        return View(model);
    }

    [HttpPost]
    [Authorization(UserTypes.PowerUser)]
    public async Task<IActionResult> Index(PlayerCreationModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            model.ErrorMessage = _localizer["MandatName"];
            SetPositionsOnModel(model);
            return View(model);
        }

        if (model.YearOfBirth == null || !ushort.TryParse(model.YearOfBirth, out var yearValue))
        {
            model.ErrorMessage = _localizer["InvalidYear"];
            SetPositionsOnModel(model);
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.ClueEn))
        {
            model.ErrorMessage = _localizer["MandatClue"];
            SetPositionsOnModel(model);
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.EasyClueEn))
        {
            model.ErrorMessage = _localizer["MandatClue"];
            SetPositionsOnModel(model);
            return View(model);
        }

        var continents = await GetContinentsAsync();

        if (model.Continent == null
            || !ulong.TryParse(model.Continent, out var continentId)
            || !continents.Any(c => continentId == c.Key))
        {
            model.ErrorMessage = _localizer["InvalidContinent"];
            SetPositionsOnModel(model);
            return View(model);
        }

        var countries = await GetCountriesAsync();

        if (model.Country == null
            || !ulong.TryParse(model.Country, out var countryId)
            || !countries.Any(c => countryId == c.Key))
        {
            model.ErrorMessage = _localizer["InvalidCountry"];
            SetPositionsOnModel(model);
            return View(model);
        }

        if (model.Position == null
            || !ulong.TryParse(model.Position, out var positionId)
            || !GetPositions().Any(p => p.Key == positionId))
        {
            model.ErrorMessage = _localizer["InvalidPosition"];
            SetPositionsOnModel(model);
            return View(model);
        }

        // les dix champs du formulaire sont facultatifs : on filtre les vides
        // avant de considerer la liste comme non nullable
        var names = new List<string?>
        {
            model.AlternativeName0, model.AlternativeName1,
            model.AlternativeName2, model.AlternativeName3,
            model.AlternativeName4, model.AlternativeName5,
            model.AlternativeName6, model.AlternativeName7,
            model.AlternativeName8, model.AlternativeName9,
        }
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct()
            .ToList();

        var clubsReferential = await GetClubsAsync();

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
            SetPositionsOnModel(model);
            return View(model);
        }

        var isAdmin = IsTypeOfUser(UserTypes.Administrator);

        var req = new PlayerRequest
        {
            SetLatestPublicationDate = isAdmin,
            AllowedNames = names,
            Clubs = clubs,
            ClueEn = model.ClueEn,
            EasyClueEn = model.EasyClueEn,
            ClueLanguages = new Dictionary<Languages, string?>
            {
                { Languages.fr, model.ClueFr }
            },
            EasyClueLanguages = new Dictionary<Languages, string?>
            {
                { Languages.fr, model.EasyClueFr }
            },
            Country = (Countries)countryId,
            Continent = (Continents)continentId,
            Name = model.Name,
            Position = (Positions)positionId,
            YearOfBirth = yearValue,
            HideCreator = model.HideCreator
        };

        var validityRequest = req.IsValid(_clock.Today, _localizer);
        if (!string.IsNullOrWhiteSpace(validityRequest))
        {
            model.ErrorMessage = string.Format(_localizer["InvalidRequest"], validityRequest);
            SetPositionsOnModel(model);
            return View(model);
        }
        else
        {
            await _playerService
                .CreatePlayerAsync(req, UserId);
            return RedirectToAction("Index", "Admin", new { withOkMessage = true });
        }
    }

    [HttpGet]
    [Authorization(UserTypes.PowerUser)]
    public async Task<IActionResult> Club([FromQuery] ulong clubId)
    {
        if (clubId > 0)
        {
            if (UserType != UserTypes.Administrator)
                return RedirectToAction("ErrorIndex", "Home");

            var club = await _internationalService
                .GetClubAsync(clubId);

            if (club == null)
                return RedirectToAction("ErrorIndex", "Home");

            var namesEn = club.NamesByLanguage[Languages.en];
            var namesFr = club.NamesByLanguage[Languages.fr];
            var model = new ClubCreationModel
            {
                MainNameEn = namesEn[0],
                MainNameFr = namesFr[0],
                AlternativeNamesEn = string.Join('\n', namesEn.Skip(1)),
                AlternativeNamesFr = string.Join('\n', namesFr.Skip(1)),
                Country = club.CountryId.ToString(),
                Id = clubId
            };

            return View("Club", model);
        }

        return View("Club", new ClubCreationModel());
    }

    [HttpPost]
    [Authorization(UserTypes.PowerUser)]
    public async Task<IActionResult> Club(ClubCreationModel model)
    {
        if (string.IsNullOrWhiteSpace(model.MainNameEn) || string.IsNullOrWhiteSpace(model.MainNameFr))
        {
            model.ErrorMessage = _localizer["ClubNameMiss"];
            return View("Club", model);
        }

        var countries = await GetCountriesAsync();

        if (model.Country == null
            || !ulong.TryParse(model.Country, out var countryId)
            || !countries.Any(c => countryId == c.Key))
        {
            model.ErrorMessage = _localizer["InvalidCountry"];
            return View("Club", model);
        }

        var request = new ClubRequest
        {
            NamesByLanguage = new Dictionary<Languages, IReadOnlyList<string>>
            {
                { Languages.en, SplitAlternativeNames(model.MainNameEn, model.AlternativeNamesEn) },
                { Languages.fr, SplitAlternativeNames(model.MainNameFr, model.AlternativeNamesFr) }
            },
            CountryId = countryId,
            Id = model.Id
        };

        var validityRequest = request.IsValid(_localizer);
        if (!string.IsNullOrWhiteSpace(validityRequest))
            model.ErrorMessage = string.Format(_localizer["InvalidRequest"], validityRequest);
        else
        {
            await _internationalService
                .CreateOrUpdateClubAsync(request);

            model = new ClubCreationModel
            {
                InfoMessage = _localizer["ClubOk"]
            };
        }

        return View("Club", model);
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> PlayerEdit(ulong playerId)
    {
        var clues = await _playerService
            .GetPlayerCluesAsync(playerId, new List<Languages> { Languages.en, Languages.fr });

        var model = new PlayerEditModel
        {
            PlayerId = playerId,
            ClueEn = clues[Languages.en].clue,
            ClueFr = clues[Languages.fr].clue,
            EasyClueEn = clues[Languages.en].easyclue,
            EasyClueFr = clues[Languages.fr].easyclue
        };

        return View("PlayerEdit", model);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> PlayerEdit(PlayerEditModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ClueEn)
            || string.IsNullOrWhiteSpace(model.ClueFr)
            || string.IsNullOrWhiteSpace(model.EasyClueEn)
            || string.IsNullOrWhiteSpace(model.EasyClueFr)
            || model.PlayerId == 0)
        {
            model.Message = "Données de formulaire invalides";
            return View("PlayerEdit", model);
        }

        await _playerService
            .UpdatePlayerCluesAsync(
                model.PlayerId,
                model.ClueEn,
                model.EasyClueEn,
                new Dictionary<Languages, string?> { { Languages.fr, model.ClueFr } },
                new Dictionary<Languages, string?> { { Languages.fr, model.EasyClueFr } });

        model.Success = true;
        model.Message = null;

        return View("PlayerEdit", model);
    }

    private async Task<List<PlayerSubmissionModel>> GetPlayerSubmissionsList()
    {
        var pls = await _playerService.GetPlayerSubmissionsAsync();

        var countries = await GetCountriesAsync();

        var continents = await GetContinentsAsync();

        return pls
            .Select(p => new PlayerSubmissionModel
            {
                AllowedNames = string.Join(';', p.AllowedNames ?? []),
                Clubs = p.Clubs,
                Clue = p.Clue,
                EasyClue = p.EasyClue,
                Country = countries[(ulong)p.Country],
                Continent = continents[(ulong)p.Continent],
                Id = p.Id,
                Login = p.Login,
                Name = p.Name,
                Position = Enum.GetValues(typeof(Positions)).Cast<Positions>().First(pp => pp == p.Position).ToString(),
                YearOfBirth = p.YearOfBirth
            })
            .ToList();
    }

    /// <summary>Nom canonique (priorite 0) suivi des alias saisis un par ligne, sans doublon ni ligne vide.</summary>
    private static IReadOnlyList<string> SplitAlternativeNames(string canonicalName, string? alternativeNames)
    {
        var aliases = (alternativeNames ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(n => !string.Equals(n, canonicalName, StringComparison.OrdinalIgnoreCase))
            .Distinct();

        return new[] { canonicalName }.Concat(aliases).ToList();
    }

    private void AddClubIfValid(List<PlayerClubRequest> clubs, string? value, IReadOnlyCollection<Club> clubsReferential, ref byte i, bool isLoan)
    {
        var id = clubsReferential.FirstOrDefault(c => value == c.Name)?.Id;
        if (id.HasValue)
        {
            clubs.Add(new PlayerClubRequest { ClubId = id.Value, HistoryPosition = i, IsLoan = isLoan });
            i++;
        }
    }

    private void SetPositionsOnModel(PlayerCreationModel model)
    {
        model.Positions = new[] { new SelectListItem("", "0") }
            .Concat(GetPositions()
                .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
            .ToList();
    }
}
