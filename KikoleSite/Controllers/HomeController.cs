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
            this.ResetAuthenticationCookie();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] int? day, [FromQuery] string errorMessageForced)
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
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            var proposalDate = DateTime.Now.Date.AddDays(-model.CurrentDay);

            var playerCreator = await _apiProvider
                .IsPlayerOfTheDayUser(proposalDate, token)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(token))
                await SetModelFromApiAsync(model, proposalDate, token, playerCreator).ConfigureAwait(false);

            var clue = await _apiProvider.GetClueAsync(proposalDate).ConfigureAwait(false);

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

            return ViewWithFullModel(model, login, clue, chart, msg, playerCreator?.Login, accepteds, pendings, playerCreator?.CanDisplayCreator == true);
        }

        [HttpPost]
        public async Task<IActionResult> Index(HomeModel model)
        {
            var (token, login) = this.GetAuthenticationCookie();

            if (model == null
                || !Enum.TryParse<ProposalType>(GetSubmitAction(), out var proposalType)
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
                .SubmitProposalAsync(now, value, model.CurrentDay,
                    proposalType,
                    token)
                .ConfigureAwait(false);

            model.IsErrorMessage = !response.Successful;
            model.MessageToDisplay = response.Successful
                ? _localizer["ValidGuess", proposalType.GetLabel(true)]
                : _localizer["InvalidGuess", proposalType.GetLabel(true), !string.IsNullOrWhiteSpace(response.Tip) ? $" {response.Tip}" : ""];

            model.Badges = response.CollectedBadges;

            var proposalDate = now.Date.AddDays(-model.CurrentDay);

            var playerCreator = await _apiProvider
                .IsPlayerOfTheDayUser(proposalDate, token)
                .ConfigureAwait(false);

            await SetModelFromApiAsync(model, proposalDate, token, playerCreator)
                .ConfigureAwait(false);

            var clue = await _apiProvider.GetClueAsync(proposalDate).ConfigureAwait(false);

            var chart = await _apiProvider.GetProposalChartAsync().ConfigureAwait(false);

            var msg = await _apiProvider
                .GetCurrentMessageAsync()
                .ConfigureAwait(false);

            var pendings = await _apiProvider
                .GetChallengesWaitingForResponseAsync(token)
                .ConfigureAwait(false);

            var accepteds = await _apiProvider
                .GetAcceptedChallengesAsync(token)
                .ConfigureAwait(false);

            return ViewWithFullModel(model, login, clue, chart, msg, playerCreator?.Login, accepteds, pendings, playerCreator?.CanDisplayCreator == true);
        }

        private IActionResult ViewWithFullModel(HomeModel model, string login,
            string clue, ProposalChart chart, string message, string creator,
            IReadOnlyCollection<Challenge> acceptedChallenges,
            IReadOnlyCollection<Challenge> pendingChallenges,
            bool canDisplayCreator)
        {
            model.PlayerCreator = canDisplayCreator ? creator : null;
            model.Message = message;
            model.LoggedAs = login;
            model.Positions = new[] { new SelectListItem("", "0") }
                .Concat(GetPositions()
                    .Select(p => new SelectListItem(p.Value, p.Key.ToString())))
                .ToList();
            model.Chart = chart;
            model.Clue = clue;
            model.NoPreviousDay = DateTime.Now.Date.AddDays(-model.CurrentDay) == chart.FirstDate;
            model.TodayChallenge = acceptedChallenges
                .SingleOrDefault(c => c.ChallengeDate == DateTime.Now.Date);
            model.HasPendingChallenges = pendingChallenges.Count > 0;
            return View(model);
        }

        private async Task SetModelFromApiAsync(HomeModel model,
            DateTime proposalDate, string authToken, PlayerCreator pc)
        {
            if (!string.IsNullOrWhiteSpace(pc?.Name))
            {
                model.SetFinalFormIsUserIsCreator(pc.Name);
                return;
            }

            var proposals = await _apiProvider
                .GetProposalsAsync(proposalDate, authToken)
                .ConfigureAwait(false);

            var countries = await _apiProvider
                .GetCountriesAsync()
                .ConfigureAwait(false);

            var positions = GetPositions();

            foreach (var p in proposals)
            {
                model.SetPropertiesFromProposal(p, countries, positions);
            }
        }
    }
}
