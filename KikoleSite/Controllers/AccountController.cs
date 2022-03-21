using System.Threading.Tasks;
using KikoleSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class AccountController : KikoleBaseController
    {
        private readonly IApiProvider _apiProvider;

        public AccountController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        public IActionResult Index()
        {
            var (token, login) = this.GetAuthenticationCookie();

            return View(new AccountModel
            {
                IsAuthenticated = login != null,
                Login = login
            });
        }

        [HttpPost]
        public async Task<IActionResult> Index(AccountModel model)
        {
            var submitFrom = GetSubmitAction();

            if (submitFrom == "logoff")
            {
                this.ResetAuthenticationCookie();
                model = new AccountModel();
            }
            else if (submitFrom == "login" || model.ForceLoginAction)
            {
                if (string.IsNullOrWhiteSpace(model.LoginSubmission)
                    || string.IsNullOrWhiteSpace(model.PasswordSubmission))
                {
                    model.Error = "Invalid form values";
                }
                else
                {
                    var (success, value) = await _apiProvider
                        .LoginAsync(model.LoginSubmission, model.PasswordSubmission)
                        .ConfigureAwait(false);
                    if (success)
                    {
                        this.SetAuthenticationCookie(value, model.LoginSubmission);
                        model.IsAuthenticated = true;
                        model.Login = model.LoginSubmission;
                    }
                    else
                        model.Error = value;
                }
            }
            else if (submitFrom == "create")
            {
                if (string.IsNullOrWhiteSpace(model.LoginCreateSubmission)
                    || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                    || !string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                {
                    model.Error = "Invalid form values";
                }
                else
                {
                    var value = await _apiProvider
                        .CreateAccountAsync(model.LoginCreateSubmission,
                            model.PasswordCreate1Submission,
                            model.RecoveryQCreate,
                            model.RecoveryACreate)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        model.Error = value;
                    }
                    else
                    {
                        return await Index(new AccountModel
                        {
                            LoginSubmission = model.LoginCreateSubmission,
                            PasswordSubmission = model.PasswordCreate1Submission,
                            ForceLoginAction = true
                        }).ConfigureAwait(false);
                    }
                }
            }
            else if (submitFrom == "changepassword")
            {
                if (string.IsNullOrWhiteSpace(model.PasswordSubmission)
                    || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                    || !string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                {
                    model.Error = "Invalid form values";
                }
                else
                {
                    var (token, login) = this.GetAuthenticationCookie();
                    var response = await _apiProvider
                        .ChangePasswordAsync(token, model.PasswordSubmission, model.PasswordCreate1Submission)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(response))
                        model.Error = response;
                    else
                    {
                        model.IsAuthenticated = true;
                        model.Login = login;
                        model.SuccessInfo = "Your password has been changed";
                    }
                }
            }

            return View(model);
        }
    }
}
