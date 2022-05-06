using System;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers
{
    public class ChallengeController : KikoleBaseController
    {
        private readonly IStringLocalizer<ChallengeController> _localizer;

        public ChallengeController(IApiProvider apiProvider, IStringLocalizer<ChallengeController> localizer)
            : base(apiProvider)
        {
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (token, login) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("ErrorIndex", "Home");

            var model = new ChallengeModel();

            await SetModelPropertiesAsync(model, token, login)
                .ConfigureAwait(false);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ChallengeModel model)
        {
            var (token, login) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("ErrorIndex", "Home");

            var action = GetSubmitAction();
            if (action == "createchallenge")
            {
                if (string.IsNullOrWhiteSpace(model.SelectedUserLogin))
                    model.ErrorMessage = _localizer["InvalidUserSelected"];
                else if (model.PointsRate <= 0 || model.PointsRate > 100)
                    model.ErrorMessage = _localizer["InvalidPointsRate"];
                else
                {
                    var u = await _apiProvider
                        .GetActiveUsersAsync()
                        .ConfigureAwait(false);

                    var uId = u.FirstOrDefault(_ => _.Login.Sanitize().Equals(model.SelectedUserLogin));

                    if (uId == null)
                        model.ErrorMessage = _localizer["InvalidUserSelected"];
                    else
                    {
                        var result = await _apiProvider
                            .CreateChallengeAsync(uId.Id, model.PointsRate, token)
                            .ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(result))
                            model.InfoMessage = _localizer["ChallengeSent"];
                        else
                            model.ErrorMessage = result;
                    }
                }
            }
            else if (action == "cancelchallenge" || action == "refusechallenge" || action == "acceptchallenge")
            {
                var isRefusal = action == "refusechallenge";
                var isAccepted = action == "acceptchallenge";

                if (model.SelectedChallengeId == 0)
                    model.ErrorMessage = _localizer["InvalidChallengeSelected"];
                else
                {
                    var result = await _apiProvider
                        .RespondToChallengeAsync(model.SelectedChallengeId, isAccepted, token)
                        .ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(result))
                    {
                        if (isRefusal)
                            model.InfoMessage = _localizer["ChallengeRefused"];
                        else if (isAccepted)
                            model.InfoMessage = _localizer["ChallengeAccepted"];
                        else
                            model.InfoMessage = _localizer["ChallengeCancelled"];
                    }
                    else
                        model.ErrorMessage = result;
                }
            }

            await SetModelPropertiesAsync(model, token, login)
                .ConfigureAwait(false);
            return View(model);
        }

        private async Task SetModelPropertiesAsync(ChallengeModel model,
            string token, string myLogin)
        {
            var users = await _apiProvider
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var requests = await _apiProvider
                .GetRequestedChallengesAsync(token)
                .ConfigureAwait(false);

            var pendings = await _apiProvider
                .GetChallengesWaitingForResponseAsync(token)
                .ConfigureAwait(false);

            var accepteds = await _apiProvider
                .GetAcceptedChallengesAsync(token)
                .ConfigureAwait(false);

            var histories = await _apiProvider
                .GetChallengesHistoryAsync(null, null, token)
                .ConfigureAwait(false);

            var usersOk = users
                .Where(u => !requests.Any(r => r.OpponentLogin == u.Login)
                    && !pendings.Any(r => r.OpponentLogin == u.Login)
                    && !accepteds.Any(r => r.ChallengeDate > DateTime.Now.Date
                        && r.OpponentLogin == u.Login)
                    && myLogin != u.Login)
                .Select(u => u.Login.Sanitize())
                .ToList();

            model.RequestedChallenges = requests;
            model.WaitingForResponseChallenges = pendings;
            model.Users = usersOk;
            model.AcceptedChallenges = accepteds;
            model.TodayChallenge = accepteds
                .SingleOrDefault(c => c.ChallengeDate == DateTime.Now.Date);
            model.ChallengesHistory = histories;
        }
    }
}
