using System;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Configuration;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Helpers;
using KikoleSite.Identity;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace KikoleSite.Controllers;

public class HomeController : KikoleBaseController
{
    private const string GiveUpSubmitAction = "GiveUp";

    private readonly IStringLocalizer<HomeController> _localizer;
    private readonly IDiscussionRepository _discussionRepository;
    private readonly IProposalService _proposalService;
    private readonly IMessageRepository _messageRepository;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RegistrationOptions _registrationOptions;

    public HomeController(IStringLocalizer<HomeController> localizer,
        IUserRepository userRepository,
        IInternationalService internationalService,
        IMessageRepository messageRepository,
        IClock clock,
        IGameCalendar gameCalendar,
        IPlayerService playerService,
        IProposalService proposalService,
        IBadgeService badgeService,
        IDiscussionRepository discussionRepository,
        SignInManager<ApplicationUser> signInManager,
        IOptions<RegistrationOptions> registrationOptions,
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
        _proposalService = proposalService;
        _messageRepository = messageRepository;
        _signInManager = signInManager;
        _registrationOptions = registrationOptions.Value;
    }

    [HttpGet]
    public IActionResult Contest()
    {
        return View();
    }

    [HttpGet]
    [Authorization]
    public IActionResult Contact()
    {
        var model = new ContactModel
        {
            LoggedAs = UserLogin
        };

        return View(model);
    }

    [HttpPost]
    [Authorization]
    public async Task<IActionResult> Contact(ContactModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
            model.ErrorMessage = _localizer["InvalidEmail"];
        else if (string.IsNullOrWhiteSpace(model.Message))
            model.ErrorMessage = _localizer["InvalidMessage"];
        else
        {
            await _discussionRepository
                .CreateDiscussionAsync(new Models.Dtos.DiscussionDto
                {
                    Email = model.Email,
                    UserId = UserId,
                    Message = model.Message
                });

            model.SuccessMessage = _localizer["SuccessContactSent"];
            model.Message = null;
        }

        model.LoggedAs = UserLogin;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Error()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> ErrorIndex()
    {
        return await Index(
                null, _localizer["AuthenticationRequired"].Value);
    }

    [HttpGet]
    public IActionResult SwitchLang([FromQuery] string redirect)
    {
        HttpContext.Request.Cookies.TryGetValue(
            CookieRequestCultureProvider.DefaultCookieName,
            out var currentLng);

        var culture = currentLng == "c=en|uic=en" ? "fr" : "en";

        HttpContext.Response.Cookies.Append
        (
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTime.MaxValue,
                    IsEssential = true,
                    Secure = false
                }
        );

        return Redirect(redirect);
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int? day, [FromQuery] string errorMessageForced)
    {
        var msg = (await _messageRepository
            .GetMessageAsync(_clock.Now))?.Message;

        var model = new HomeModel
        {
            CurrentDate = _clock.Today,
            Points = ScoreCalculator.BasePoints,
            Message = msg,
            RegistrationInviteEnabled = _registrationOptions.InviteEnabled
        };

        if (day.HasValue
            && model.CurrentDay != day.Value
            && (day.Value >= 0 || IsTypeOfUser(UserTypes.Administrator)))
        {
            model.CurrentDay = day.Value;
            if (model.DateOfDay < _gameCalendar.HiddenDate)
            {
                return RedirectToAction("Index");
            }

            if (model.DateOfDay == _gameCalendar.HiddenDate)
            {
                var displayHidden = await _playerService
                    .CanDisplayHiddenPlayerAsync(UserId);
                if (!displayHidden)
                {
                    model.DisplayHiddenPageAsHidden = true;
                    return View(model);
                }
            }
        }

        return await SetAndGetViewModelAsync(
                errorMessageForced,
                model
            );
    }

    [HttpPost]
    [Authorization]
    public async Task<IActionResult> Index(HomeModel model)
    {
        if (model == null)
        {
            return Redirect("/");
        }

        model.CurrentDate = _clock.Today;

        var proposalType = ProposalTypes.Name;
        var value = string.Empty;
        var submitAction = GetSubmitAction();
        var isGiveUp = submitAction == GiveUpSubmitAction;

        if (!isGiveUp)
        {
            if (!Enum.TryParse(submitAction, out proposalType))
            {
                return Redirect("/");
            }

            value = model.GetValueFromProposalType(proposalType);
            if (!IsValidInput(proposalType, value))
            {
                return await Index(
                        model.CurrentDay, _localizer["InvalidRequest"]);
            }
        }

        var daysBefore = (uint)model.CurrentDay;

        var pInfo = await _playerService
            .GetPlayerOfTheDayFullInfoAsync(_clock.Today.AddDays(-daysBefore));

        var ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        ProposalResponse response;
        if (isGiveUp)
        {
            do
            {
                var innerPr = new ProposalRequest
                {
                    DaysBeforeNow = daysBefore,
                    ProposalDateTime = _clock.Now,
                    Ip = ip,
                    ProposalType = ProposalTypes.Name,
                    Value = $"GiveUp-{_clock.Now:hhmmss}"
                };

                var (responseTmp, _, _) = await _proposalService
                    .ManageProposalResponseAsync(
                        innerPr,
                        UserId,
                        pInfo);

                response = responseTmp;
            }
            while (response.TotalPoints > 0);

            var pr = new ProposalRequest
            {
                DaysBeforeNow = daysBefore,
                ProposalDateTime = _clock.Now,
                Ip = ip,
                ProposalType = ProposalTypes.Name,
                Value = pInfo.Player.Name
            };

            (response, _, _) = await _proposalService
                .ManageProposalResponseAsync(
                    pr,
                    UserId,
                    pInfo);

            // no badges management in case of giveup
        }
        else
        {
            var request = new ProposalRequest
            {
                DaysBeforeNow = daysBefore,
                ProposalDateTime = _clock.Now,
                Ip = ip,
                ProposalType = proposalType,
                Value = value
            };

            var (responseTmp, proposalsAlready, leader) = await _proposalService
                .ManageProposalResponseAsync(request, UserId, pInfo);

            response = responseTmp;

            if (leader != null)
            {
                var leaderBadges = await _badgeService
                    .PrepareNewLeaderBadgesAsync(leader, pInfo.Player, proposalsAlready, ViewHelper.GetLanguage());

                foreach (var b in leaderBadges)
                    response.AddBadge(b);
            }

            var proposalBadges = await _badgeService
                .PrepareNonLeaderBadgesAsync(UserId, request, ViewHelper.GetLanguage());

            foreach (var b in proposalBadges)
                response.AddBadge(b);

            model.Badges = response.CollectedBadges;
        }

        model.IsErrorMessage = !response.Successful;
        if (!proposalType.CanBeMiss())
            model.MessageToDisplay = response.Tip;
        else
        {
            model.MessageToDisplay = response.Successful
                ? _localizer["ValidGuess", proposalType.GetLabel(true)]
                : _localizer["InvalidGuess", proposalType.GetLabel(true), !string.IsNullOrWhiteSpace(response.Tip) ? $" {response.Tip}" : ""];
        }

        model.Message = (await _messageRepository
            .GetMessageAsync(_clock.Now))?.Message;

        return await SetAndGetViewModelAsync(
                null,
                model);
    }

    private static bool IsValidInput(ProposalTypes proposalType, string? value)
    {
        switch (proposalType)
        {
            case ProposalTypes.Year:
                return int.TryParse(value, out var yearV) && yearV >= 1850 && yearV <= 2010;
            case ProposalTypes.Position:
                return value.IsEnumValue<Positions>();
            case ProposalTypes.Continent:
                return value.IsEnumValue<Continents>();
            case ProposalTypes.Country:
                return value.IsEnumValue<Countries>();
            case ProposalTypes.Club:
                // l'identifiant vient d'une selection d'autocompletion, jamais saisi a la main
                return !string.IsNullOrWhiteSpace(value) && ulong.TryParse(value, out _);
            case ProposalTypes.Name:
                return !string.IsNullOrWhiteSpace(value) && !int.TryParse(value, out _);
            default:
                return true;
        }
    }

    private async Task<IActionResult> SetAndGetViewModelAsync(
        string? errorMessageForced,
        HomeModel model)
    {
        var proposalDate = model.DateOfDay;

        var playerCreator = UserId > 0
            ? await _playerService
                .GetPlayerOfTheDayFromUserPovAsync(UserId, proposalDate)
            : null;

        var clue = await _playerService
            .GetPlayerClueAsync(proposalDate, false, ViewHelper.GetLanguage());

        var easyClue = await _playerService
            .GetPlayerClueAsync(proposalDate, true, ViewHelper.GetLanguage());

        if (UserId > 0)
        {
            if (!string.IsNullOrWhiteSpace(playerCreator?.Name))
            {
                model.SetFinalFormIsUserIsCreator(playerCreator.Name, playerCreator.AllowedNames ?? []);
            }
            else
            {
                var proposals = await _proposalService
                    .GetProposalsAsync(proposalDate, UserId);

                var countries = await GetCountriesAsync();

                var continents = await GetContinentsAsync();

                var positions = GetPositions();

                var language = ViewHelper.GetLanguage();
                var clubs = (await _internationalService.GetClubsAsync())
                    .ToDictionary(c => c.Id, c => c.GetCanonicalName(language));

                foreach (var p in proposals)
                    model.SetPropertiesFromProposal(p, countries, continents, positions, clubs, easyClue);
            }
        }

        if (!string.IsNullOrWhiteSpace(errorMessageForced))
        {
            model.IsErrorMessageForced = true;
            model.MessageToDisplay = errorMessageForced;
        }

        var isPowerUser = IsTypeOfUser(UserTypes.PowerUser);

        var isAdminUser = IsTypeOfUser(UserTypes.Administrator);

        if (!string.IsNullOrWhiteSpace(model.PlayerName) && string.IsNullOrWhiteSpace(model.EasyClue))
            model.EasyClue = easyClue;

        model.PlayerCreator = playerCreator?.CanDisplayCreator == true ? playerCreator?.Login : null;
        model.LoggedAs = UserLogin;
        model.Positions = new[] { new SelectListItem("", "0") }
            .Concat(GetPositions()
                .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
            .ToList();
        model.Clue = clue;
        model.NoPreviousDay = _clock.Today.AddDays(-model.CurrentDay) == _gameCalendar.FirstDate;
        model.CanCreateClub = isPowerUser;
        model.IsAdmin = isAdminUser;
        model.PlayerId = playerCreator?.PlayerId ?? 0;

        if (!string.IsNullOrWhiteSpace(model.PlayerName))
        {
            var pp = await _playerService
                .GetPlayerOfTheDayFullInfoAsync(proposalDate);

            var countries = await GetCountriesAsync();

            var continents = await GetContinentsAsync();

            var countryContinents = await _internationalService.GetCountryContinentsAsync();

            model.CountryName = countries.FirstOrDefault(c => c.Key == pp.Player.CountryId).Value;
            if (pp.Player.AlternativeCountryId.HasValue
                && countries.TryGetValue(pp.Player.AlternativeCountryId.Value, out var altCountryName))
                model.CountryName += $" / {altCountryName}";

            var mainContinentId = countryContinents[pp.Player.CountryId];
            model.ContinentName = continents.FirstOrDefault(c => c.Key == mainContinentId).Value;
            if (pp.Player.AlternativeCountryId.HasValue)
            {
                var altContinentId = countryContinents[pp.Player.AlternativeCountryId.Value];
                if (altContinentId != mainContinentId && continents.TryGetValue(altContinentId, out var altContinentName))
                    model.ContinentName += $" / {altContinentName}";
            }
            model.Position = ((Positions)pp.Player.PositionId).GetLabel();
            model.KnownPlayerClubs = pp.PlayerClubs.Select(pc => new PlayerClub(pc, pp.Clubs)).ToList();
            model.BirthYear = pp.Player.YearOfBirth.ToNaString();
        }

        return View("Index", model);
    }
}
