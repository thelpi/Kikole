using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Interfaces.Services;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;
using KikoleSite.Api.Models.Requests;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers
{
    public class HomeController : KikoleBaseController
    {
        private readonly IStringLocalizer<HomeController> _localizer;
        private readonly IDiscussionRepository _discussionRepository;
        private readonly IProposalService _proposalService;
        private readonly IMessageRepository _messageRepository;

        public HomeController(IStringLocalizer<HomeController> localizer,
            IUserRepository userRepository,
            ICrypter crypter,
            IStringLocalizer<Translations> resources,
            IInternationalRepository internationalRepository,
            IMessageRepository messageRepository,
            IClock clock,
            IPlayerService playerService,
            IClubRepository clubRepository,
            IProposalService proposalService,
            IBadgeService badgeService,
            IDiscussionRepository discussionRepository)
            : base(userRepository,
                crypter,
                resources,
                internationalRepository,
                clock,
                playerService,
                clubRepository,
                badgeService)
        {
            _localizer = localizer;
            _discussionRepository = discussionRepository;
            _proposalService = proposalService;
            _messageRepository = messageRepository;
        }

        [HttpGet]
        public IActionResult Contest()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            var (token, login) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("ErrorIndex", "Home");
            }

            var model = new ContactModel
            {
                LoggedAs = login
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Contact(ContactModel model)
        {
            var (token, login) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("ErrorIndex", "Home");

            if (string.IsNullOrWhiteSpace(model.Email))
                model.ErrorMessage = _localizer["InvalidEmail"];
            else if (string.IsNullOrWhiteSpace(model.Message))
                model.ErrorMessage = _localizer["InvalidMessage"];
            else
            {
                var result = await CreateDiscussionAsync(
                        model.Email, model.Message, token)
                    .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(result))
                {
                    model.SuccessMessage = _localizer["SuccessContactSent"];
                    model.Message = null;
                }
                else
                    model.SuccessMessage = result;
            }

            model.LoggedAs = login;
            return View(model);
        }

        [HttpGet]
        public IActionResult Error()
        {
            ResetAuthenticationCookie();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ErrorIndex()
        {
            return await Index(
                    null, _localizer["AuthenticationRequired"].Value)
                .ConfigureAwait(false);
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
            var (token, login) = GetAuthenticationCookie();

            var msg = await GetCurrentMessageAsync().ConfigureAwait(false);

            var chart = await GetProposalChartAsync().ConfigureAwait(false);

            var model = new HomeModel { Points = chart.BasePoints, Message = msg };

            if (day.HasValue
                && model.CurrentDay != day.Value
                && (day.Value >= 0 || await IsAdminUserAsync(token).ConfigureAwait(false)))
            {
                var dt = DateTime.Now.Date.AddDays(-day.Value);
                if (dt >= chart.FirstDate.Date)
                {
                    model = new HomeModel
                    {
                        Points = chart.BasePoints,
                        CurrentDay = day.Value
                    };
                }
                else if (dt == chart.FirstDate.Date.AddDays(-1))
                {
                    var known = await GetUserKnownPlayersAsync(token).ConfigureAwait(false);
                    if (known.Count == ((DateTime.Now.Date - chart.FirstDate.Date).Days + 1))
                    {
                        model = new HomeModel
                        {
                            Points = chart.BasePoints,
                            CurrentDay = day.Value
                        };
                    }
                    else
                    {
                        model = new HomeModel { AlmostThere = true };
                        return View(model);
                    }
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            return await SetAndGetViewModelAsync(
                    errorMessageForced,
                    token,
                    login,
                    chart,
                    model,
                    DateTime.Now.Date.AddDays(-model.CurrentDay)
                ).ConfigureAwait(false);
        }

        [HttpPost]
        public async Task<IActionResult> Index(HomeModel model)
        {
            var (token, login) = GetAuthenticationCookie();

            if (model == null
                || !Enum.TryParse<ProposalTypes>(GetSubmitAction(), out var proposalType)
                || string.IsNullOrWhiteSpace(token))
            {
                return Redirect("/");
            }

            var msg = await GetCurrentMessageAsync().ConfigureAwait(false);

            model.Message = msg;

            var value = model.GetValueFromProposalType(proposalType);
            if (string.IsNullOrWhiteSpace(value))
            {
                return await Index(model.CurrentDay, _localizer["InvalidRequest"])
                    .ConfigureAwait(false);
            }

            var now = DateTime.Now;

            var response = await SubmitProposalAsync(
                    value,
                    (uint)model.CurrentDay,
                    proposalType,
                    token,
                    Request.HttpContext.Connection.RemoteIpAddress.ToString())
                .ConfigureAwait(false);

            model.IsErrorMessage = !response.Successful;
            if (proposalType == ProposalTypes.Clue)
                model.MessageToDisplay = response.Tip;
            else
            {
                model.MessageToDisplay = response.Successful
                    ? _localizer["ValidGuess", proposalType.GetLabel(true)]
                    : _localizer["InvalidGuess", proposalType.GetLabel(true), !string.IsNullOrWhiteSpace(response.Tip) ? $" {response.Tip}" : ""];
            }

            model.Badges = response.CollectedBadges;

            return await SetAndGetViewModelAsync(
                    null,
                    token,
                    login,
                    await GetProposalChartAsync().ConfigureAwait(false),
                    model,
                    now.Date.AddDays(-model.CurrentDay))
                .ConfigureAwait(false);
        }

        private async Task<IActionResult> SetAndGetViewModelAsync(
            string errorMessageForced,
            string token,
            string login,
            ProposalChart chart,
            HomeModel model,
            DateTime proposalDate)
        {
            var playerCreator = await IsPlayerOfTheDayUser(
                    proposalDate, token)
                .ConfigureAwait(false);

            var clue = await GetClueAsync(
                    proposalDate, false)
                .ConfigureAwait(false);

            var easyClue = await GetClueAsync(
                    proposalDate, true)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (!string.IsNullOrWhiteSpace(playerCreator?.Name))
                {
                    model.SetFinalFormIsUserIsCreator(playerCreator.Name);
                }
                else
                {
                    var proposals = await GetProposalsAsync(
                            proposalDate, token)
                        .ConfigureAwait(false);

                    var countries = await GetCountriesAsync().ConfigureAwait(false);

                    var positions = GetPositions();

                    foreach (var p in proposals)
                        model.SetPropertiesFromProposal(p, countries, positions, easyClue);
                }
            }

            if (!string.IsNullOrWhiteSpace(errorMessageForced))
            {
                model.IsErrorMessageForced = true;
                model.MessageToDisplay = errorMessageForced;
            }

            var isPowerUser = await IsPowerUserAsync(token)
                .ConfigureAwait(false);

            var isAdminUser = await IsAdminUserAsync(token)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(model.PlayerName) && string.IsNullOrWhiteSpace(model.EasyClue))
                model.EasyClue = easyClue;

            model.PlayerCreator = playerCreator?.CanDisplayCreator == true ? playerCreator?.Login : null;
            model.LoggedAs = login;
            model.Positions = new[] { new SelectListItem("", "0") }
                .Concat(GetPositions()
                    .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
                .ToList();
            model.Chart = chart;
            model.Clue = clue;
            model.NoPreviousDay = DateTime.Now.Date.AddDays(-model.CurrentDay) == chart.FirstDate;
            model.CanCreateClub = isPowerUser;
            model.IsAdmin = isAdminUser;
            model.PlayerId = playerCreator?.PlayerId ?? 0;

            if (!string.IsNullOrWhiteSpace(model.PlayerName))
            {
                var pp = await GetFullPlayerAsync(proposalDate).ConfigureAwait(false);

                var countries = await GetCountriesAsync().ConfigureAwait(false);

                model.CountryName = countries.FirstOrDefault(c => c.Key == pp.Player.CountryId).Value;
                model.Position = ((Positions)pp.Player.PositionId).GetLabel();
                model.KnownPlayerClubs = pp.PlayerClubs.Select(pc => new PlayerClub(pc, pp.Clubs)).ToList();
                model.BirthYear = pp.Player.YearOfBirth.ToNaString();
            }

            return View("Index", model);
        }

        private async Task<string> GetCurrentMessageAsync()
        {
            var message = await _messageRepository
                .GetMessageAsync(_clock.Now)
                .ConfigureAwait(false);

            return message?.Message;
        }

        private async Task<string> CreateDiscussionAsync(string email, string message, string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return _resources["InvalidUser"];

            await _discussionRepository
                .CreateDiscussionAsync(new Api.Models.Dtos.DiscussionDto
                {
                    Email = email,
                    UserId = userId,
                    Message = message
                })
                .ConfigureAwait(false);

            return null;
        }

        private async Task<Api.Models.Dtos.PlayerFullDto> GetFullPlayerAsync(DateTime date)
        {
            return await _playerService
                .GetPlayerOfTheDayFullInfoAsync(date)
                .ConfigureAwait(false);
        }

        private async Task<ProposalResponse> SubmitProposalAsync(string value, uint daysBeforeNow, ProposalTypes proposalType, string authToken, string ip)
        {
            var request = BaseProposalRequest.Create(_clock.Now, value, daysBeforeNow, proposalType, ip);

            if (request == null)
                return null;

            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return null;

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return null;

            var firstDate = await _playerService
                .GetFirstSubmittedPlayerDateAsync(true)
                .ConfigureAwait(false);

            if (request.PlayerSubmissionDate < firstDate.Date || request.PlayerSubmissionDate > _clock.Today)
                return null;

            var pInfo = await _playerService
                .GetPlayerOfTheDayFullInfoAsync(request.PlayerSubmissionDate)
                .ConfigureAwait(false);

            var (response, proposalsAlready, leader) = await _proposalService
                .ManageProposalResponseAsync(request, userId, pInfo)
                .ConfigureAwait(false);

            if (leader != null)
            {
                var leaderBadges = await _badgeService
                    .PrepareNewLeaderBadgesAsync(leader, pInfo.Player, proposalsAlready, Helper.GetLanguage())
                    .ConfigureAwait(false);

                foreach (var b in leaderBadges)
                    response.AddBadge(b);
            }

            var proposalBadges = await _badgeService
                .PrepareNonLeaderBadgesAsync(userId, request, Helper.GetLanguage())
                .ConfigureAwait(false);

            foreach (var b in proposalBadges)
                response.AddBadge(b);

            return response;
        }

        private async Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(DateTime proposalDate, string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return null;

            var proposals = await _proposalService
                .GetProposalsAsync(proposalDate, userId)
                .ConfigureAwait(false);

            return proposals;
        }

        private async Task<string> GetClueAsync(DateTime proposalDate, bool isEasy)
        {
            var clue = await _playerService
                .GetPlayerClueAsync(proposalDate, isEasy, Helper.GetLanguage())
                .ConfigureAwait(false);

            return clue;
        }

        private async Task<PlayerCreator> IsPlayerOfTheDayUser(DateTime proposalDate, string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return null;

            var p = await _playerService
                .GetPlayerOfTheDayFromUserPovAsync(userId, proposalDate)
                .ConfigureAwait(false);

            return p;
        }
    }
}
