using System;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class AccountController : Controller
    {
        const string CookieName = "AccountForm";

        private readonly ApiProvider _apiProvider;

        public AccountController(ApiProvider apiProvider)
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
                    || string.IsNullOrWhiteSpace(model.Password1Submission)
                    || string.IsNullOrWhiteSpace(model.Password2Submission)
                    || !string.Equals(model.Password1Submission, model.Password2Submission))
                {
                    model.Error = "Invalid form values";
                }
                else
                {
                    var result = await _apiProvider
                        .LoginAsync(model.LoginSubmission, model.Password1Submission)
                        .ConfigureAwait(false);
                    if (result.Item2)
                    {
                        SetCookie(CookieName, $"{result.Item1}§§§{model.LoginSubmission}", DateTime.Now.AddMonths(1));
                        model.IsAuthenticated = true;
                        model.Login = model.LoginSubmission;
                    }
                    else
                        model.Error = result.Item1;
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
