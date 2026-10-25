using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Helpers;
using KikoleSite.Identity;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace KikoleSite.Controllers;

public class AdminController : KikoleBaseController
{
    // formats acceptes pour un indice (image deja geree ailleurs, + audio/video
    // uploades localement, cf. UploadClueMedia) et plafond de taille (indices courts :
    // quelques secondes de son, une dizaine de secondes de video)
    private static readonly string[] AllowedClueMediaExtensions =
        [".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".svg", ".mp3", ".mp4"];
    // sous-ensemble reserve aux administrateurs (cf. UploadClueMedia) - un PowerUser reste
    // limite au texte, aux liens et aux images
    private static readonly string[] AudioVideoExtensions = [".mp3", ".mp4"];
    private const long MaxClueMediaFileSizeBytes = 15 * 1024 * 1024;
    private const int MaxDisabledReasonLength = 500;
    private const int LoginSuggestionsCount = 10;

    private readonly IStringLocalizer<AdminController> _localizer;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailProtector _emailProtector;
    private readonly ILogger<AdminController> _logger;
    private readonly IErrorJournal _errorJournal;
    private readonly IDiscussionService _discussionService;
    private readonly ILeaderService _leaderService;
    private readonly IMessageRepository _messageRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(IStringLocalizer<AdminController> localizer,
        IUserRepository userRepository,
        IInternationalService internationalService,
        IMessageRepository messageRepository,
        IClock clock,
        IGameCalendar gameCalendar,
        IPlayerService playerService,
        IBadgeService badgeService,
        ILeaderService leaderService,
        IDiscussionService discussionService,
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment webHostEnvironment,
        UserManager<ApplicationUser> userManager,
        IEmailProtector emailProtector,
        ILogger<AdminController> logger,
        IErrorJournal errorJournal)
        : base(userRepository,
            internationalService,
            clock,
            gameCalendar,
            playerService,
            badgeService,
            httpContextAccessor)
    {
        _localizer = localizer;
        _discussionService = discussionService;
        _leaderService = leaderService;
        _messageRepository = messageRepository;
        _webHostEnvironment = webHostEnvironment;
        _userManager = userManager;
        _emailProtector = emailProtector;
        _logger = logger;
        _errorJournal = errorJournal;
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Actions()
    {
        return await RenderActionsAsync(new AdminModel());
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> RecomputeBadges()
    {
        await _badgeService
            .ResetBadgesAsync(ViewHelper.GetLanguage(), await _internationalService.GetCountryContinentsAsync());

        return await RenderActionsAsync(new AdminModel());
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> RecomputeLeaders()
    {
        await _leaderService
            .ComputeMissingLeadersAsync(await _internationalService.GetCountryContinentsAsync());

        return await RenderActionsAsync(new AdminModel());
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> ReassignPlayers()
    {
        await _playerService
            .ReassignPlayersOfTheDayAsync();

        return await RenderActionsAsync(new AdminModel());
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public IActionResult Errors()
    {
        return View(_errorJournal.GetLatest());
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Users(UserListQuery query)
    {
        return await RenderUsersAsync(query, null, null);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<JsonResult> AutoCompleteUserLogins(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return Json(Array.Empty<string>());

        return Json(await _userRepository.SearchLoginsAsync(prefix, LoginSuggestionsCount));
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> DisableUser(UserActionRequest request)
    {
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > MaxDisabledReasonLength)
            return await RenderUsersAsync(request, null, _localizer["DisableReasonInvalid"]);

        var target = await GetManageableUserAsync(request.UserId);
        if (target == null)
            return await RenderUsersAsync(request, null, _localizer["UserNotManageable"]);

        await _userRepository.DisableUserAsync(target.Id, reason);

        return await RenderUsersAsync(request, _localizer["UserDisabled", target.Login], null);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> ForceUserPassword(UserActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return await RenderUsersAsync(request, null, _localizer["PasswordRequired"]);

        if (request.NewPassword != request.NewPasswordConfirm)
            return await RenderUsersAsync(request, null, _localizer["PasswordMismatch"]);

        var target = await GetManageableUserAsync(request.UserId);
        var user = target == null ? null : await _userManager.FindByIdAsync(target.Id.ToString());
        if (user == null)
            return await RenderUsersAsync(request, null, _localizer["UserNotManageable"]);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!reset.Succeeded)
            return await RenderUsersAsync(request, null, MapPasswordError(reset));

        // le nouveau mot de passe leve aussi un verrouillage en cours (comme la reinitialisation par email)
        await _userManager.ResetAccessFailedCountAsync(user);

        return await RenderUsersAsync(request, _localizer["PasswordForced", user.UserName ?? string.Empty], null);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> ChangeUserType(UserActionRequest request)
    {
        if (request.NewType is not (UserTypes.StandardUser or UserTypes.PowerUser))
            return await RenderUsersAsync(request, null, _localizer["UserNotManageable"]);

        var target = await GetManageableUserAsync(request.UserId);
        if (target == null || target.UserTypeId == (ulong)request.NewType)
            return await RenderUsersAsync(request, null, _localizer["UserNotManageable"]);

        await _userRepository.ChangeUserTypeAsync(target.Id, request.NewType.Value);

        return await RenderUsersAsync(request, _localizer["UserTypeChanged", target.Login], null);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> DeleteUser(UserActionRequest request)
    {
        var target = await _userRepository.GetUserByIdIncludingDisabledAsync(request.UserId);
        if (target == null || target.UserTypeId == (ulong)UserTypes.Administrator)
            return await RenderUsersAsync(request, null, _localizer["UserNotDeletable"]);

        if (!string.Equals(request.LoginConfirmation?.Trim(), target.Login, StringComparison.OrdinalIgnoreCase))
            return await RenderUsersAsync(request, null, _localizer["LoginConfirmationMismatch"]);

        if (!await _userRepository.DeleteUserWithAllDataAsync(target.Id))
            return await RenderUsersAsync(request, null, _localizer["UserNotDeletable"]);

        // identifiants seuls : ni login ni email ne doivent survivre dans les journaux
        _logger.LogInformation("Compte {DeletedUserId} supprimé par l'administrateur {AdminUserId}.", target.Id, UserId);

        return await RenderUsersAsync(request, _localizer["UserDeleted", target.Login], null);
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Planning(string? done)
    {
        var model = await BuildPlanningModelAsync();
        if (done == "saved")
            model.Feedback = _localizer["PlanningSaved"];

        return View(model);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Planning(PlanningSaveModel save)
    {
        var result = await _playerService.ReorderScheduledPlayersAsync(save.Ids, save.Snapshot ?? string.Empty);
        if (result == ScheduleReorderResult.Saved)
            return RedirectToAction("Planning", new { done = "saved" });

        var model = await BuildPlanningModelAsync();
        model.Error = _localizer["PlanningOutOfDate"];
        return View(model);
    }

    private async Task<PlanningModel> BuildPlanningModelAsync()
    {
        var players = await _playerService.GetScheduledPlayersAsync();

        return new PlanningModel
        {
            FirstDate = _clock.Tomorrow,
            Snapshot = ScheduleSnapshot.From(players),
            Players = players.Select(p => new PlanningRow(p.Id, p.Name)).ToList()
        };
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Announcements(string? done)
    {
        var now = _clock.Now;
        var messages = await _messageRepository.GetMessagesAsync();
        var displayed = await _messageRepository.GetMessageAsync(now);

        return View(new AnnouncementsModel
        {
            Feedback = done switch
            {
                "saved" => _localizer["AnnouncementSaved"].Value,
                "deleted" => _localizer["AnnouncementDeleted"].Value,
                _ => null
            },
            Rows = messages
                .Select(m => new AnnouncementRow(m.Id, m.Message, m.DisplayFrom, m.DisplayTo, GetAnnouncementStatus(m, now, displayed?.Id)))
                .ToList()
        });
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Announcement(ulong id)
    {
        if (id == 0)
        {
            // default : from now without ms to tomorrow 23:59:59
            return View(new AnnouncementModel { DisplayFrom = _clock.NowSeconds, DisplayTo = _clock.TomorrowEnd });
        }

        var message = await _messageRepository.GetMessageByIdAsync(id);
        if (message == null)
            return RedirectToAction("Announcements");

        return View(new AnnouncementModel
        {
            Id = message.Id,
            Message = message.Message,
            DisplayFrom = message.DisplayFrom,
            DisplayTo = message.DisplayTo
        });
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Announcement(AnnouncementModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Message))
        {
            model.ErrorMessage = _localizer["AnnouncementMessageMandatory"];
            return View(model);
        }

        if (model.DisplayFrom.HasValue && model.DisplayTo.HasValue && model.DisplayTo <= model.DisplayFrom)
        {
            model.ErrorMessage = _localizer["AnnouncementDatesInvalid"];
            return View(model);
        }

        var message = new Models.Dtos.MessageDto
        {
            Id = model.Id,
            Message = model.Message.Trim(),
            DisplayFrom = model.DisplayFrom,
            DisplayTo = model.DisplayTo,
            CreationDate = _clock.Now
        };

        if (model.Id == 0)
        {
            await _messageRepository.InsertMessageAsync(message);
        }
        else
        {
            if (await _messageRepository.GetMessageByIdAsync(model.Id) == null)
                return RedirectToAction("Announcements");

            await _messageRepository.UpdateMessageAsync(message);
        }

        return RedirectToAction("Announcements", new { done = "saved" });
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> DeleteAnnouncement(ulong id)
    {
        await _messageRepository.DeleteMessageAsync(id);

        return RedirectToAction("Announcements", new { done = "deleted" });
    }

    // meme regle de selection que MessageRepository.GetMessageAsync : parmi les annonces
    // actives, une seule est affichee (celle que la requete retient)
    internal static AnnouncementStatus GetAnnouncementStatus(Models.Dtos.MessageDto message, DateTime now, ulong? displayedId)
    {
        if (message.DisplayFrom > now)
            return AnnouncementStatus.Scheduled;

        if (message.DisplayTo <= now)
            return AnnouncementStatus.Expired;

        return message.Id == displayedId ? AnnouncementStatus.Displayed : AnnouncementStatus.Superseded;
    }

    private async Task<IActionResult> RenderActionsAsync(AdminModel model)
    {
        model.HasPendingSubmissions = await _playerService.HasPendingSubmissionsAsync();
        model.HasUnreadMessages = await _discussionService.HasUnreadMessagesForAdminAsync();

        return View("Actions", model);
    }

    // un compte actif et non administrateur : seuls ceux-la se gerent depuis la page des utilisateurs
    private async Task<Models.Dtos.UserDto?> GetManageableUserAsync(ulong userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        return user == null || user.UserTypeId == (ulong)UserTypes.Administrator ? null : user;
    }

    private async Task<IActionResult> RenderUsersAsync(UserListQuery query, string? feedback, string? error)
    {
        // un administrateur n'est jamais listable : filtrer dessus ne donnerait que du vide
        var type = query.Type == UserTypes.Administrator ? null : query.Type;
        var page = Math.Max(1, query.Page);

        var (users, total) = await _userRepository
            .SearchUsersAsync(query.Login, query.Status, type, query.Desc, page, UsersModel.PageSize);

        // la page demandee n'existe plus (liste raccourcie entre-temps) : derniere page
        if (users.Count == 0 && total > 0)
        {
            page = (total + UsersModel.PageSize - 1) / UsersModel.PageSize;
            (users, total) = await _userRepository
                .SearchUsersAsync(query.Login, query.Status, type, query.Desc, page, UsersModel.PageSize);
        }

        var model = new UsersModel
        {
            Query = new UserListQuery
            {
                Login = query.Login,
                Status = query.Status,
                Type = type,
                Desc = query.Desc,
                Page = page
            },
            Rows = [.. users.Select(u => UserAdminRow.From(u, _emailProtector.Decrypt(u.EmailEncrypted)))],
            TotalCount = total,
            Feedback = feedback,
            Error = error
        };

        return View("Users", model);
    }

    private string MapPasswordError(IdentityResult result)
    {
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordTooShort)))
            return _localizer["PasswordTooShort"];

        if (result.Errors.Any(e => e.Code == HibpPasswordValidator.PwnedPasswordErrorCode))
            return _localizer["PasswordCompromised"];

        return _localizer["PasswordRejected"];
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Discussions()
    {
        var discussions = await _discussionService.GetAllDiscussionsAsync();
        return View(new AdminDiscussionsModel { Discussions = discussions });
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> StartDiscussion(ulong userId, string? login)
    {
        var target = userId > 0
            ? await GetManageableUserAsync(userId)
            : await FindManageableUserByLoginAsync(login);

        if (target == null)
        {
            return View("Discussions", new AdminDiscussionsModel
            {
                Discussions = await _discussionService.GetAllDiscussionsAsync(),
                ErrorMessage = _localizer["UserNotContactable"]
            });
        }

        var discussionId = await _discussionService.GetOrCreateDiscussionAsync(target.Id);

        return RedirectToAction("Discussion", "Admin", new { discussionId });
    }

    private async Task<Models.Dtos.UserDto?> FindManageableUserByLoginAsync(string? login)
    {
        if (string.IsNullOrWhiteSpace(login))
            return null;

        var user = await _userRepository.GetUserByNormalizedLoginAsync(login.Trim().ToUpperInvariant());

        return user == null ? null : await GetManageableUserAsync(user.Id);
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Discussion(ulong discussionId)
    {
        var summary = (await _discussionService.GetAllDiscussionsAsync())
            .FirstOrDefault(d => d.DiscussionId == discussionId);

        if (summary == null)
            return RedirectToAction("Discussions", "Admin");

        var messages = await _discussionService.GetThreadForAdminAsync(discussionId);

        return View(new AdminDiscussionModel
        {
            DiscussionId = discussionId,
            UserLogin = summary.UserLogin,
            Messages = messages
        });
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> Discussion(AdminDiscussionModel model)
    {
        if (string.IsNullOrWhiteSpace(model.NewMessage))
            model.ErrorMessage = _localizer["InvalidMessage"];
        else
            await _discussionService.PostAdminReplyAsync(model.DiscussionId, model.NewMessage);

        var summary = (await _discussionService.GetAllDiscussionsAsync())
            .FirstOrDefault(d => d.DiscussionId == model.DiscussionId);

        if (summary != null)
            model.UserLogin = summary.UserLogin;
        model.Messages = await _discussionService.GetThreadForAdminAsync(model.DiscussionId);
        model.NewMessage = null;
        ModelState.Remove(nameof(AdminDiscussionModel.NewMessage));

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
    public async Task<IActionResult> AcceptPlayer(PlayerSubmissionsModel model)
    {
        return await RespondToSubmissionAsync(model, isAccepted: true);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> RefusePlayer(PlayerSubmissionsModel model)
    {
        return await RespondToSubmissionAsync(model, isAccepted: false);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> ChoosePlayer(PlayerSubmissionsModel model)
    {
        var redirect = await EnsurePendingSubmissionsAsync(model);
        if (redirect != null)
            return redirect;

        if (model.SelectedPlayer == null)
        {
            model.ErrorMessage = _localizer["InvalidSelectedPlayer"];
            model.SelectedId = 0;
        }

        return View("PlayerSubmission", model);
    }

    private async Task<IActionResult> RespondToSubmissionAsync(PlayerSubmissionsModel model, bool isAccepted)
    {
        var redirect = await EnsurePendingSubmissionsAsync(model);
        if (redirect != null)
            return redirect;

        // action deja reservee aux administrateurs ([Authorization(UserTypes.Administrator)]
        // sur AcceptPlayer/RefusePlayer) : pas de garde-fou de palier supplementaire ici
        DateOnly? forcedPublicationDate = null;
        if (isAccepted && !TryParseForcedPublicationDate(model.PublicationDate, out forcedPublicationDate))
        {
            model.ErrorMessage = _localizer["InvalidPublicationDate"];
            return View("PlayerSubmission", model);
        }

        var request = new PlayerSubmissionValidationRequest
        {
            PublicationDate = forcedPublicationDate,
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
            IsAccepted = isAccepted,
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

        return View("PlayerSubmission", model);
    }

    /// <summary>
    /// Recharge la liste des soumissions en attente sur le modele (necessaire a
    /// <see cref="PlayerSubmissionsModel.SelectedPlayer"/>) ; redirige vers la liste s'il
    /// n'y en a plus, comme au chargement initial.
    /// </summary>
    private async Task<IActionResult?> EnsurePendingSubmissionsAsync(PlayerSubmissionsModel model)
    {
        model.Players = await GetPlayerSubmissionsList();

        return model.Players.Count == 0
            ? RedirectToAction("PlayerSubmission", "Admin")
            : null;
    }

    /// <summary>
    /// Parse une date de publication forcee, reservee aux administrateurs (creation
    /// directe via <see cref="Index(PlayerCreationModel)"/> ou acceptation d'une
    /// soumission via <see cref="RespondToSubmissionAsync"/>) : doit etre strictement
    /// dans le futur, jamais aujourd'hui ni le passe (deja joues - le service ne decale
    /// jamais que des jours futurs). Champ vide = rien a forcer, ce n'est pas une erreur.
    /// </summary>
    private bool TryParseForcedPublicationDate(string? rawValue, out DateOnly? publicationDate)
    {
        publicationDate = null;

        if (string.IsNullOrWhiteSpace(rawValue))
            return true;

        if (!DateOnly.TryParse(rawValue, out var parsedDate) || parsedDate <= _clock.Today)
            return false;

        publicationDate = parsedDate;
        return true;
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
        var isAdmin = IsTypeOfUser(UserTypes.Administrator);
        model.DisplayPlayerSubmissionLink = isAdmin;

        var (request, error) = await BuildPlayerRequestAsync(model, isAdmin, true);
        if (request == null)
        {
            model.ErrorMessage = error;
            SetPositionsOnModel(model);
            return View(model);
        }

        await _playerService
            .CreatePlayerAsync(request, UserId);
        return RedirectToAction("Index", "Admin", new { withOkMessage = true });
    }

    private async Task<(PlayerRequest? Request, string? Error)> BuildPlayerRequestAsync(
        PlayerCreationModel model, bool isAdmin, bool withPublicationDate)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return (null, _localizer["MandatName"].Value);
        }

        if (model.YearOfBirth == null || !ushort.TryParse(model.YearOfBirth, out var yearValue))
        {
            return (null, _localizer["InvalidYear"].Value);
        }

        if (string.IsNullOrWhiteSpace(model.ClueEn))
        {
            return (null, _localizer["MandatClue"].Value);
        }

        if (string.IsNullOrWhiteSpace(model.EasyClueEn))
        {
            return (null, _localizer["MandatEasyClue"].Value);
        }

        var countries = await GetCountriesAsync();

        if (model.Country == null
            || !ulong.TryParse(model.Country, out var countryId)
            || !countries.Any(c => countryId == c.Key))
        {
            return (null, _localizer["InvalidNationality"].Value);
        }

        // facultatif : uniquement pour un joueur ayant represente une nation sportive
        // disparue en plus de son pays actuel (ex. RDA puis Allemagne) ; un nom saisi
        // sans suggestion choisie (identifiant vide) est une erreur, pas un champ vide
        ulong? alternativeCountryId = null;
        if (!string.IsNullOrWhiteSpace(model.AlternativeCountry) || !string.IsNullOrWhiteSpace(model.AlternativeCountryName))
        {
            if (!ulong.TryParse(model.AlternativeCountry, out var parsedAlternativeCountryId)
                || !countries.Any(c => parsedAlternativeCountryId == c.Key))
            {
                return (null, _localizer["InvalidAlternativeNationality"].Value);
            }
            alternativeCountryId = parsedAlternativeCountryId;
        }

        if (string.IsNullOrWhiteSpace(model.Position) || model.Position == "0")
        {
            return (null, _localizer["MandatPosition"].Value);
        }

        if (!ulong.TryParse(model.Position, out var positionId)
            || !GetPositions().Any(p => p.Key == positionId))
        {
            return (null, _localizer["InvalidPosition"].Value);
        }

        // facultatif : uniquement si le joueur occupe plausiblement deux postes differents
        // (ex. milieu offensif / attaquant) ; "0" est la valeur de l'option vide du select
        ulong? alternativePositionId = null;
        if (!string.IsNullOrWhiteSpace(model.AlternativePosition) && model.AlternativePosition != "0")
        {
            if (!ulong.TryParse(model.AlternativePosition, out var parsedAlternativePositionId)
                || !GetPositions().Any(p => p.Key == parsedAlternativePositionId))
            {
                return (null, _localizer["InvalidAlternativePosition"].Value);
            }
            alternativePositionId = parsedAlternativePositionId;
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

        if (names.Count == 0)
        {
            return (null, _localizer["MandatAlternativeName"].Value);
        }

        var clubsReferential = await GetClubsAsync();

        var unknownClubLine = FindUnknownClubLine(model, clubsReferential);
        if (unknownClubLine > 0)
        {
            return (null, string.Format(_localizer["UnknownClub"].Value, unknownClubLine));
        }

        byte iPos = 1;
        var clubs = new List<PlayerClubRequest>();
        AddClubIfValid(clubs, model.Club0Id, clubsReferential, ref iPos, model.IsLoan0);
        AddClubIfValid(clubs, model.Club1Id, clubsReferential, ref iPos, model.IsLoan1);
        AddClubIfValid(clubs, model.Club2Id, clubsReferential, ref iPos, model.IsLoan2);
        AddClubIfValid(clubs, model.Club3Id, clubsReferential, ref iPos, model.IsLoan3);
        AddClubIfValid(clubs, model.Club4Id, clubsReferential, ref iPos, model.IsLoan4);
        AddClubIfValid(clubs, model.Club5Id, clubsReferential, ref iPos, model.IsLoan5);
        AddClubIfValid(clubs, model.Club6Id, clubsReferential, ref iPos, model.IsLoan6);
        AddClubIfValid(clubs, model.Club7Id, clubsReferential, ref iPos, model.IsLoan7);
        AddClubIfValid(clubs, model.Club8Id, clubsReferential, ref iPos, model.IsLoan8);
        AddClubIfValid(clubs, model.Club9Id, clubsReferential, ref iPos, model.IsLoan9);
        AddClubIfValid(clubs, model.Club10Id, clubsReferential, ref iPos, model.IsLoan10);
        AddClubIfValid(clubs, model.Club11Id, clubsReferential, ref iPos, model.IsLoan11);
        AddClubIfValid(clubs, model.Club12Id, clubsReferential, ref iPos, model.IsLoan12);
        AddClubIfValid(clubs, model.Club13Id, clubsReferential, ref iPos, model.IsLoan13);
        AddClubIfValid(clubs, model.Club14Id, clubsReferential, ref iPos, model.IsLoan14);

        if (clubs.Count == 0)
        {
            return (null, _localizer["OneClubMin"].Value);
        }

        // reserve aux administrateurs : force la date de publication au lieu du "bout de
        // chaine" habituel ; un PowerUser n'a de toute facon pas ce champ dans son
        // formulaire, mais on l'ignore explicitement aussi cote serveur par securite
        DateOnly? forcedPublicationDate = null;
        if (isAdmin && withPublicationDate && !TryParseForcedPublicationDate(model.PublicationDate, out forcedPublicationDate))
        {
            return (null, _localizer["InvalidPublicationDate"].Value);
        }

        var req = new PlayerRequest
        {
            SetLatestPublicationDate = isAdmin && withPublicationDate,
            PublicationDate = forcedPublicationDate,
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
            AlternativeCountry = alternativeCountryId.HasValue ? (Countries)alternativeCountryId.Value : null,
            Name = model.Name,
            Position = (Positions)positionId,
            AlternativePosition = alternativePositionId.HasValue ? (Positions)alternativePositionId.Value : null,
            YearOfBirth = yearValue,
            HideCreator = model.HideCreator,
            IsWoman = model.IsWoman
        };

        var validityRequest = req.IsValid(_clock.Today, _localizer);
        return string.IsNullOrWhiteSpace(validityRequest)
            ? (req, null)
            : (null, string.Format(_localizer["InvalidRequest"], validityRequest));
    }
    // upload d'un media d'indice (image/audio/video), utilise par le formulaire de
    // creation (Index) et d'edition (PlayerEdit) : le fichier est stocke dans wwwroot,
    // le chemin renvoye remplace la valeur du champ texte correspondant (site.js)
    [HttpPost]
    [Authorization(UserTypes.PowerUser)]
    public async Task<IActionResult> UploadClueMedia(IFormFile? file)
    {
        if (file == null || file.Length == 0 || file.Length > MaxClueMediaFileSizeBytes)
            return BadRequest();

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)
            || !AllowedClueMediaExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        // l'audio/la video sont reserves aux administrateurs : un PowerUser ne peut fournir
        // que du texte, des liens et des images (cf. AllowedClueMediaExtensions).
        // StatusCode(403) plutot que Forbid() : sous l'authentification par cookie,
        // Forbid() est intercepte et transforme en redirection (302) vers la page
        // "acces refuse" au lieu d'un vrai 403 - un fetch/$.ajax suit la redirection et
        // voit un succes (200) plutot qu'une erreur, ce qui a ete confirme en verifiant
        // en direct (aucun fichier ecrit malgre un statut 200 rapporte par fetch).
        if (AudioVideoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
            && !IsTypeOfUser(UserTypes.Administrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "media", "clues");
        Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(folderPath, fileName);

        using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        return Json(new { path = $"/media/clues/{fileName}" });
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

            return View("Club", await BuildClubEditModelAsync(club));
        }

        return View("Club", new ClubCreationModel { CanEditExisting = IsTypeOfUser(UserTypes.Administrator) });
    }

    private async Task<ClubCreationModel> BuildClubEditModelAsync(Club club)
    {
        var namesEn = club.NamesByLanguage[Languages.en];
        var namesFr = club.NamesByLanguage[Languages.fr];

        var model = new ClubCreationModel
        {
            MainNameEn = namesEn[0],
            MainNameFr = namesFr[0],
            AlternativeNamesEn = string.Join('\n', namesEn.Skip(1)),
            AlternativeNamesFr = string.Join('\n', namesFr.Skip(1)),
            Country = club.CountryId.ToString(),
            Id = club.Id
        };

        await FillClubAdminInfoAsync(model);

        return model;
    }

    // nom du pays (le champ de saisie est vide sinon) et usage du club, pour le seul administrateur
    private async Task FillClubAdminInfoAsync(ClubCreationModel model)
    {
        if (!IsTypeOfUser(UserTypes.Administrator))
            return;

        model.CanEditExisting = true;

        if (ulong.TryParse(model.Country, out var countryId))
            model.CountryName = (await GetCountriesAsync()).GetValueOrDefault(countryId);

        if (model.Id > 0)
        {
            var usage = await _internationalService.GetClubUsageAsync(model.Id);
            model.PlayerNames = usage.PlayerNames;
            model.IncorrectProposals = usage.IncorrectProposals;
            model.CorrectProposals = usage.CorrectProposals;
        }
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> DeleteClub(ulong clubId)
    {
        var club = await _internationalService.GetClubAsync(clubId);
        var result = await _internationalService.DeleteClubAsync(clubId);

        switch (result.Outcome)
        {
            case ClubDeletionOutcomes.Deleted:
                ModelState.Clear();
                return View("Club", new ClubCreationModel
                {
                    CanEditExisting = true,
                    InfoMessage = string.Format(_localizer["ClubDeleted"], club?.Name, result.ConvertedProposals)
                });

            case ClubDeletionOutcomes.NotFound:
                return View("Club", new ClubCreationModel
                {
                    CanEditExisting = true,
                    ErrorMessage = _localizer["ClubNotFound"]
                });

            default:
                var editModel = await BuildClubEditModelAsync(club!);
                editModel.ErrorMessage = _localizer[result.Outcome == ClubDeletionOutcomes.UsedByPlayers ? "ClubUsedByPlayers" : "ClubUsedByProposals"];
                return View("Club", editModel);
        }
    }

    [HttpPost]
    [Authorization(UserTypes.PowerUser)]
    public async Task<IActionResult> Club(ClubCreationModel model)
    {
        // meme garde que le GET : seul un administrateur modifie un club existant
        if (model.Id > 0 && !IsTypeOfUser(UserTypes.Administrator))
            return RedirectToAction("ErrorIndex", "Home");

        if (string.IsNullOrWhiteSpace(model.MainNameEn) || string.IsNullOrWhiteSpace(model.MainNameFr))
        {
            model.ErrorMessage = _localizer["ClubNameMiss"];
            await FillClubAdminInfoAsync(model);
            return View("Club", model);
        }

        var countries = await GetCountriesAsync();

        if (model.Country == null
            || !ulong.TryParse(model.Country, out var countryId)
            || !countries.Any(c => countryId == c.Key))
        {
            model.ErrorMessage = _localizer["InvalidCountry"];
            await FillClubAdminInfoAsync(model);
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
        else if (await _internationalService.ClubNameAlreadyExistsAsync(request))
            model.ErrorMessage = _localizer["ClubNameAlreadyExists"];
        else
        {
            await _internationalService
                .CreateOrUpdateClubAsync(request);

            // les champs re-affiches viennent d'abord de ce que le formulaire a poste
            // (ModelState), pas du nouveau modele : sans ca le formulaire resterait rempli
            ModelState.Clear();

            // une modification reste sur la fiche du club, une creation repart d'un formulaire vide
            var saved = model.Id > 0 ? await _internationalService.GetClubAsync(model.Id) : null;
            model = saved != null
                ? await BuildClubEditModelAsync(saved)
                : new ClubCreationModel { CanEditExisting = IsTypeOfUser(UserTypes.Administrator) };
            model.InfoMessage = _localizer["ClubOk"];
            return View("Club", model);
        }

        await FillClubAdminInfoAsync(model);
        return View("Club", model);
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> UpcomingPlayers(bool saved)
    {
        var players = await _playerService.GetEditablePlayersAsync();

        return View(new UpcomingPlayersModel
        {
            Saved = saved,
            Players = players
                .Select(p => new UpcomingPlayerRow(p.Id, p.Name, p.PublicationDate))
                .ToList()
        });
    }

    [HttpGet]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> PlayerEdit(ulong playerId)
    {
        var data = await _playerService.GetEditablePlayerAsync(playerId);
        if (data == null)
            return RedirectToAction("UpcomingPlayers");

        var model = await BuildEditModelAsync(data);
        SetPositionsOnModel(model);
        return View("Index", model);
    }

    [HttpPost]
    [Authorization(UserTypes.Administrator)]
    public async Task<IActionResult> PlayerEdit(PlayerCreationModel model)
    {
        if (!model.PlayerId.HasValue)
            return RedirectToAction("UpcomingPlayers");

        model.DisplayPlayerSubmissionLink = true;

        var (request, error) = await BuildPlayerRequestAsync(model, true, false);
        if (request != null)
        {
            if (await _playerService.UpdatePlayerAsync(model.PlayerId.Value, request))
                return RedirectToAction("UpcomingPlayers", new { saved = true });

            error = _localizer["PlayerNotEditable"].Value;
        }

        model.ErrorMessage = error;
        SetPositionsOnModel(model);
        return View("Index", model);
    }

    private async Task<PlayerCreationModel> BuildEditModelAsync(PlayerEditData data)
    {
        var player = data.Player;
        var countries = await GetCountriesAsync();
        var clubsReferential = await GetClubsAsync();
        var language = ViewHelper.GetLanguage();

        var model = new PlayerCreationModel
        {
            PlayerId = player.Id,
            DisplayPlayerSubmissionLink = true,
            Name = player.Name,
            YearOfBirth = player.YearOfBirth.ToString(),
            Country = player.CountryId.ToString(),
            CountryName = countries.GetValueOrDefault(player.CountryId),
            AlternativeCountry = player.AlternativeCountryId?.ToString(),
            AlternativeCountryName = player.AlternativeCountryId.HasValue ? countries.GetValueOrDefault(player.AlternativeCountryId.Value) : null,
            Position = player.PositionId.ToString(),
            AlternativePosition = player.AlternativePositionId?.ToString(),
            ClueEn = player.Clue,
            EasyClueEn = player.EasyClue,
            ClueFr = data.ClueFr,
            EasyClueFr = data.EasyClueFr,
            HideCreator = player.HideCreator,
            IsWoman = player.IsWoman
        };

        // les noms acceptes sont stockes normalises : on ne retrouve que cette forme ; les alias
        // non latins abimes par l'ancienne normalisation ("?? ??") ne sont pas repris
        var sanitizedName = player.Name.Sanitize();
        var alternativeNames = player.AllowedNames.Disjoin()
            .Where(n => n != sanitizedName && n.HasReadableText())
            .Take(10)
            .ToList();
        for (var i = 0; i < alternativeNames.Count; i++)
            SetIndexedProperty(model, "AlternativeName", i, string.Empty, alternativeNames[i]);

        for (var i = 0; i < data.Clubs.Count && i < 15; i++)
        {
            var playerClub = data.Clubs[i];
            var club = clubsReferential.FirstOrDefault(c => c.Id == playerClub.ClubId);
            SetIndexedProperty(model, "Club", i, "Id", playerClub.ClubId.ToString());
            SetIndexedProperty(model, "Club", i, "Name", club?.GetCanonicalName(language));
            SetIndexedProperty(model, "IsLoan", i, string.Empty, playerClub.IsLoan);
        }

        return model;
    }

    private static void SetIndexedProperty(PlayerCreationModel model, string prefix, int index, string suffix, object? value)
    {
        typeof(PlayerCreationModel)
            .GetProperty($"{prefix}{index}{suffix}")!
            .SetValue(model, value);
    }
    private async Task<List<PlayerSubmissionModel>> GetPlayerSubmissionsList()
    {
        // countries/continents sont independants de pls (et l'un de l'autre) : partent
        // en // de la chaine countryContinents -> pls, qui elle reste sequentielle
        var countriesTask = GetCountriesAsync();

        var continentsTask = GetContinentsAsync();

        var plsTask = GetSubmissionsAsync();

        await Task.WhenAll(countriesTask, continentsTask, plsTask);

        var pls = await plsTask;

        var countries = await countriesTask;

        var continents = await continentsTask;

        async Task<IReadOnlyCollection<Player>> GetSubmissionsAsync()
        {
            return await _playerService.GetPlayerSubmissionsAsync(await _internationalService.GetCountryContinentsAsync());
        }

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
    internal static IReadOnlyList<string> SplitAlternativeNames(string canonicalName, string? alternativeNames)
    {
        var aliases = (alternativeNames ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(n => !string.Equals(n, canonicalName, StringComparison.OrdinalIgnoreCase))
            .Distinct();

        return new[] { canonicalName }.Concat(aliases).ToList();
    }

    // numero (1 a 15) de la premiere ligne dont le nom est saisi sans club reconnu, 0 sinon
    internal static int FindUnknownClubLine(PlayerCreationModel model, IReadOnlyCollection<Club> clubsReferential)
    {
        var names = new[] { model.Club0Name, model.Club1Name, model.Club2Name, model.Club3Name, model.Club4Name, model.Club5Name, model.Club6Name, model.Club7Name, model.Club8Name, model.Club9Name, model.Club10Name, model.Club11Name, model.Club12Name, model.Club13Name, model.Club14Name };
        var ids = new[] { model.Club0Id, model.Club1Id, model.Club2Id, model.Club3Id, model.Club4Id, model.Club5Id, model.Club6Id, model.Club7Id, model.Club8Id, model.Club9Id, model.Club10Id, model.Club11Id, model.Club12Id, model.Club13Id, model.Club14Id };

        for (var i = 0; i < names.Length; i++)
        {
            var isKnown = ulong.TryParse(ids[i], out var clubId) && clubsReferential.Any(c => c.Id == clubId);
            if (!string.IsNullOrWhiteSpace(names[i]) && !isKnown)
                return i + 1;
        }

        return 0;
    }

    internal static void AddClubIfValid(List<PlayerClubRequest> clubs, string? clubIdValue, IReadOnlyCollection<Club> clubsReferential, ref byte i, bool isLoan)
    {
        if (ulong.TryParse(clubIdValue, out var clubId) && clubsReferential.Any(c => c.Id == clubId))
        {
            clubs.Add(new PlayerClubRequest { ClubId = clubId, HistoryPosition = i, IsLoan = isLoan });
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
