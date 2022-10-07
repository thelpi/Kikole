using System;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Helpers;
using KikoleSite.Interfaces;
using KikoleSite.Interfaces.Repositories;
using KikoleSite.Interfaces.Services;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
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
        private const string GiveUpSubmitAction = "GiveUp";

        private readonly IStringLocalizer<HomeController> _localizer;
        private readonly IDiscussionRepository _discussionRepository;
        private readonly IProposalService _proposalService;
        private readonly IMessageRepository _messageRepository;

        public HomeController(IStringLocalizer<HomeController> localizer,
            IUserRepository userRepository,
            ICrypter crypter,
            IInternationalRepository internationalRepository,
            IMessageRepository messageRepository,
            IClock clock,
            IPlayerService playerService,
            IClubRepository clubRepository,
            IProposalService proposalService,
            IBadgeService badgeService,
            IDiscussionRepository discussionRepository,
            IHttpContextAccessor httpContextAccessor)
            : base(userRepository,
                crypter,
                internationalRepository,
                clock,
                playerService,
                clubRepository,
                badgeService,
                httpContextAccessor)
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
                    })
                    .ConfigureAwait(false);

                model.SuccessMessage = _localizer["SuccessContactSent"];
                model.Message = null;
            }

            model.LoggedAs = UserLogin;
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
            var msg = (await _messageRepository
                .GetMessageAsync(_clock.Now)
                .ConfigureAwait(false))?.Message;

            var chart = await GetProposalChartAsync().ConfigureAwait(false);

            var model = new HomeModel { Points = chart.BasePoints, Message = msg };

            if (day.HasValue
                && model.CurrentDay != day.Value
                && (day.Value >= 0 || IsTypeOfUser(UserTypes.Administrator)))
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
                    var foundAll = await _playerService
                        .GetHasFoundEveryPlayerAsync(UserId)
                        .ConfigureAwait(false);
                    if (foundAll)
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
                    chart,
                    model,
                    DateTime.Now.Date.AddDays(-model.CurrentDay)
                ).ConfigureAwait(false);
        }

        [HttpPost]
        [Authorization]
        public async Task<IActionResult> Index(HomeModel model)
        {
            if (model == null)
            {
                return Redirect("/");
            }

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
                if (string.IsNullOrWhiteSpace(value))
                {
                    return await Index(
                            model.CurrentDay, _localizer["InvalidRequest"])
                        .ConfigureAwait(false);
                }
            }

            var daysBefore = (uint)model.CurrentDay;

            var pInfo = await _playerService
                .GetPlayerOfTheDayFullInfoAsync(_clock.Now.AddDays(-daysBefore).Date)
                .ConfigureAwait(false);

            var ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();

            ProposalResponse response;
            if (isGiveUp)
            {
                do
                {
                    var (responseTmp, _, _) = await _proposalService
                        .ManageProposalResponseAsync(
                            BaseProposalRequest.Create(_clock.Now, $"GiveUp-{_clock.Now:hhmmss}", daysBefore, ProposalTypes.Name, ip),
                            UserId,
                            pInfo)
                        .ConfigureAwait(false);

                    response = responseTmp;
                }
                while (response.TotalPoints > 0);

                (response, _, _) = await _proposalService
                    .ManageProposalResponseAsync(
                        BaseProposalRequest.Create(_clock.Now, pInfo.Player.Name, daysBefore, ProposalTypes.Name, ip),
                        UserId,
                        pInfo)
                    .ConfigureAwait(false);

                // no badges management in case of giveup
            }
            else
            {
                var request = BaseProposalRequest.Create(_clock.Now, value, daysBefore, proposalType, ip);

                var (responseTmp, proposalsAlready, leader) = await _proposalService
                    .ManageProposalResponseAsync(request, UserId, pInfo)
                    .ConfigureAwait(false);

                response = responseTmp;

                if (leader != null)
                {
                    var leaderBadges = await _badgeService
                        .PrepareNewLeaderBadgesAsync(leader, pInfo.Player, proposalsAlready, ViewHelper.GetLanguage())
                        .ConfigureAwait(false);

                    foreach (var b in leaderBadges)
                        response.AddBadge(b);
                }

                var proposalBadges = await _badgeService
                    .PrepareNonLeaderBadgesAsync(UserId, request, ViewHelper.GetLanguage())
                    .ConfigureAwait(false);

                foreach (var b in proposalBadges)
                    response.AddBadge(b);

                model.Badges = response.CollectedBadges;
            }

            model.IsErrorMessage = !response.Successful;
            if (proposalType == ProposalTypes.Clue)
                model.MessageToDisplay = response.Tip;
            else
            {
                model.MessageToDisplay = response.Successful
                    ? _localizer["ValidGuess", proposalType.GetLabel(true)]
                    : _localizer["InvalidGuess", proposalType.GetLabel(true), !string.IsNullOrWhiteSpace(response.Tip) ? $" {response.Tip}" : ""];
            }

            model.Message = (await _messageRepository
                .GetMessageAsync(_clock.Now)
                .ConfigureAwait(false))?.Message;

            return await SetAndGetViewModelAsync(
                    null,
                    await GetProposalChartAsync().ConfigureAwait(false),
                    model,
                    _clock.Today.AddDays(-model.CurrentDay))
                .ConfigureAwait(false);
        }

        private async Task<IActionResult> SetAndGetViewModelAsync(
            string errorMessageForced,
            ProposalChart chart,
            HomeModel model,
            DateTime proposalDate)
        {
            var playerCreator = UserId > 0
                ? await _playerService
                    .GetPlayerOfTheDayFromUserPovAsync(UserId, proposalDate)
                    .ConfigureAwait(false)
                : null;

            var clue = await _playerService
                .GetPlayerClueAsync(proposalDate, false, ViewHelper.GetLanguage())
                .ConfigureAwait(false);

            var easyClue = await _playerService
                .GetPlayerClueAsync(proposalDate, true, ViewHelper.GetLanguage())
                .ConfigureAwait(false);

            if (UserId > 0)
            {
                if (!string.IsNullOrWhiteSpace(playerCreator?.Name))
                {
                    model.SetFinalFormIsUserIsCreator(playerCreator.Name);
                }
                else
                {
                    var proposals = await _proposalService
                        .GetProposalsAsync(proposalDate, UserId)
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
            model.Chart = chart;
            model.Clue = clue;
            model.NoPreviousDay = DateTime.Now.Date.AddDays(-model.CurrentDay) == chart.FirstDate;
            model.CanCreateClub = isPowerUser;
            model.IsAdmin = isAdminUser;
            model.PlayerId = playerCreator?.PlayerId ?? 0;

            if (!string.IsNullOrWhiteSpace(model.PlayerName))
            {
                var pp = await _playerService
                    .GetPlayerOfTheDayFullInfoAsync(proposalDate)
                    .ConfigureAwait(false);

                var countries = await GetCountriesAsync().ConfigureAwait(false);

                model.CountryName = countries.FirstOrDefault(c => c.Key == pp.Player.CountryId).Value;
                model.Position = ((Positions)pp.Player.PositionId).GetLabel();
                model.KnownPlayerClubs = pp.PlayerClubs.Select(pc => new PlayerClub(pc, pp.Clubs)).ToList();
                model.BirthYear = pp.Player.YearOfBirth.ToNaString();
            }

            return View("Index", model);
        }
    }
}
