using System;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers
{
    public class HomeController : KikoleBaseController
    {
        private readonly IStringLocalizer<HomeController> _localizer;

        public HomeController(IApiProvider apiProvider, IStringLocalizer<HomeController> localizer)
            : base(apiProvider)
        {
            _localizer = localizer;
        }

        [HttpGet]
        public IActionResult Error()
        {
            ResetAuthenticationCookie();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int? day, [FromQuery] string errorMessageForced)
        {
            var (token, login) = GetAuthenticationCookie();

            var chart = await _apiProvider
                .GetProposalChartAsync()
                .ConfigureAwait(false);

            var model = new HomeModel { Points = chart.BasePoints };

            if (day.HasValue
                && model.CurrentDay != day.Value
                && day.Value >= 0)
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
                    var known = await _apiProvider
                        .GetUserKnownPlayersAsync(token)
                        .ConfigureAwait(false);
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

            var value = model.GetValueFromProposalType(proposalType);
            if (string.IsNullOrWhiteSpace(value))
            {
                return await Index(model.CurrentDay, _localizer["InvalidRequest"])
                    .ConfigureAwait(false);
            }

            var now = DateTime.Now;

            var response = await _apiProvider
                .SubmitProposalAsync(value, (uint)model.CurrentDay,
                    proposalType,
                    token)
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
                    await _apiProvider.GetProposalChartAsync().ConfigureAwait(false),
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
            var playerCreator = await _apiProvider
                .IsPlayerOfTheDayUser(proposalDate, token)
                .ConfigureAwait(false);

            var clue = await _apiProvider.GetClueAsync(proposalDate, false).ConfigureAwait(false);

            var easyClue = await _apiProvider.GetClueAsync(proposalDate, true).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (!string.IsNullOrWhiteSpace(playerCreator?.Name))
                {
                    model.SetFinalFormIsUserIsCreator(playerCreator.Name);
                }
                else
                {
                    var proposals = await _apiProvider
                        .GetProposalsAsync(proposalDate, token)
                        .ConfigureAwait(false);

                    var countries = await _apiProvider
                        .GetCountriesAsync()
                        .ConfigureAwait(false);

                    var positions = GetPositions();

                    foreach (var p in proposals)
                        model.SetPropertiesFromProposal(p, countries, positions, easyClue);
                }
            }

            if (!string.IsNullOrWhiteSpace(errorMessageForced))
            {
                model.IsErrorMessage = true;
                model.MessageToDisplay = errorMessageForced;
            }

            var msg = await _apiProvider
                .GetCurrentMessageAsync()
                .ConfigureAwait(false);

            var pendings = await _apiProvider
                .GetChallengesWaitingForResponseAsync(token)
                .ConfigureAwait(false);

            var accepteds = await _apiProvider
                .GetAcceptedChallengesAsync(token)
                .ConfigureAwait(false);

            var isPowerUser = await _apiProvider
                .IsPowerUserAsync(token)
                .ConfigureAwait(false);

            model.PlayerCreator = playerCreator?.CanDisplayCreator == true ? playerCreator?.Login : null;
            model.Message = msg;
            model.LoggedAs = login;
            model.Positions = new[] { new SelectListItem("", "0") }
                .Concat(GetPositions()
                    .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
                .ToList();
            model.Chart = chart;
            model.Clue = clue;
            model.NoPreviousDay = DateTime.Now.Date.AddDays(-model.CurrentDay) == chart.FirstDate;
            model.TodayChallenge = accepteds
                .SingleOrDefault(c => c.ChallengeDate == DateTime.Now.Date);
            model.HasPendingChallenges = pendings.Count > 0;
            model.CanCreateClub = isPowerUser;
            return View(model);
        }
    }
}
