using System;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class AccountController : Controller
    {
        const string CookieName = "AccountForm";

        private readonly IApiProvider _apiProvider;

        public AccountController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        public IActionResult Index()
        {
            var model = GetCookieModel();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(AccountModel model)
        {
            var submitFrom = HttpContext.Request.Form.Keys.Single(x => x.StartsWith("submit-")).Split('-')[1];

            if (submitFrom == "logoff")
            {
                Response.Cookies.Delete(CookieName);
                model = new AccountModel();
            }
            else if (submitFrom == "login")
            {
                if (string.IsNullOrWhiteSpace(model.LoginSubmission)
                    || string.IsNullOrWhiteSpace(model.PasswordSubmission))
                {
                    model.Error = "Invalid form values";
                }
                else
                {
                    var result = await _apiProvider
                        .LoginAsync(model.LoginSubmission, model.PasswordSubmission)
                        .ConfigureAwait(false);
                    if (result.success)
                    {
                        SetCookie(CookieName, $"{result.value}§§§{model.LoginSubmission}", DateTime.Now.AddMonths(1));
                        model.IsAuthenticated = true;
                        model.Login = model.LoginSubmission;
                    }
                    else
                        model.Error = result.value;
                }
            }
            else if (submitFrom == "create")
            {
                if (string.IsNullOrWhiteSpace(model.LoginCreateSubmission)
                    || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                    || string.IsNullOrWhiteSpace(model.PasswordCreate2Submission)
                    || !string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                {
                    model.Error = "Invalid form values";
                }
                else
                {
                    var createResul = await _apiProvider
                        .CreateAccountAsync(model.LoginCreateSubmission,
                            model.PasswordCreate1Submission,
                            model.RecoveryQCreate,
                            model.RecoveryACreate)
                        .ConfigureAwait(false);
                    if (!createResul.Item1)
                    {
                        model.Error = createResul.Item2;
                    }
                    else
                    {
                        model = new AccountModel
                        {
                            SuccessInfo = "You can now login with this account",
                            LoginSubmission = model.LoginCreateSubmission,
                            PasswordSubmission = model.PasswordCreate1Submission
                        };
                    }
                }
            }

            return View(model);
        }

        private void SetCookie(string cookieName, string cookieValue, DateTime expiration)
        {
            Response.Cookies.Delete(cookieName);
            var option = new CookieOptions
            {
                Expires = expiration,
                IsEssential = true,
                Secure = false
            };
            Response.Cookies.Append(cookieName, cookieValue, option);
        }

        private AccountModel GetCookieModel()
        {
            var model = new AccountModel();

            if (Request.Cookies.TryGetValue(CookieName, out string cookieValue))
            {
                var cookieParts = cookieValue.Split("§§§");
                if (cookieParts.Length > 1)
                {
                    model.IsAuthenticated = true;
                    model.Login = cookieParts[1];
                }
            }

            return model;
        }
    }
}
