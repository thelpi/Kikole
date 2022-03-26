using System;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class ChallengeController : KikoleBaseController
    {
        public ChallengeController(IApiProvider apiProvider)
            : base(apiProvider)
        { }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (token, login) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
                RedirectToAction("Index", "Home");

            var model = new ChallengeModel();

            await SetModelPropertiesAsync(
                    model, DateTime.Now.AddDays(1).Date, token, login)
                .ConfigureAwait(false);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ChallengeModel model)
        {
            var (token, login) = GetAuthenticationCookie();
            if (string.IsNullOrWhiteSpace(token))
                RedirectToAction("Index", "Home");

            var action = GetSubmitAction();
            if (action == "createchallenge")
            {
                if (model.SelectedUserId == 0)
                    model.ErrorMessage = "Invalid user selected";
                else if (model.PointsRate <= 0 || model.PointsRate > 100)
                    model.ErrorMessage = "Invalid points rate";
                else
                {
                    var result = await _apiProvider
                        .CreateChallengeAsync(model.SelectedUserId, model.PointsRate, token)
                        .ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(result))
                        model.InfoMessage = "Success! Challenge sent to your opponent";
                    else
                        model.ErrorMessage = $"Error: {result}";
                }
            }
            else if (action == "cancelchallenge" || action == "refusechallenge" || action == "acceptchallenge")
            {
                var isRefusal = action == "refusechallenge";
                var isAccepted = action == "acceptchallenge";

                if (model.SelectedChallengeId == 0)
                    model.ErrorMessage = "Invalid challenge selected";
                else
                {
                    var result = await _apiProvider
                        .RespondToChallengeAsync(model.SelectedChallengeId, isAccepted, token)
                        .ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(result))
                        model.InfoMessage = $"Success! Challenge has been {(isRefusal ? "refused" : (isAccepted ? "accepted" : "cancelled"))}";
                    else
                        model.ErrorMessage = $"Error: {result}";
                }
            }

            await SetModelPropertiesAsync(
                    model, DateTime.Now.AddDays(1).Date, token, login)
                .ConfigureAwait(false);
            return View(model);
        }

        private async Task SetModelPropertiesAsync(ChallengeModel model,
            DateTime date, string token, string myLogin)
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

            var usersOk = users
                .Where(u => !requests.Any(r => r.OpponentLogin == u.Login)
                    && !pendings.Any(r => r.OpponentLogin == u.Login)
                    && myLogin != u.Login)
                .ToList();
            
            model.RequestedChallenges = requests;
            model.WaitingForResponseChallenges = pendings;
            model.Users = usersOk;
            model.AcceptedChallenge = await _apiProvider
                .GetAcceptedChallengeAsync(date, token)
                .ConfigureAwait(false);
        }
    }
}
