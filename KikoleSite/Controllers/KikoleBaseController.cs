using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api;
using KikoleSite.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public abstract class KikoleBaseController : Controller
    {
        internal const string _cryptedAuthenticationCookieName = "AccountFormCrypt";
        internal const string CookiePartsSeparator = "§§§";

        protected readonly IApiProvider _apiProvider;

        protected KikoleBaseController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        protected string GetSubmitAction()
        {
            var submitKeys = HttpContext.Request.Form.Keys.Where(x => x.StartsWith("submit-"));

            if (submitKeys.Count() != 1)
                return null;

            var submitKeySplit = submitKeys.First().Split('-');
            if (submitKeySplit.Length != 2)
                return null;

            return submitKeySplit[1];
        }

        [HttpPost]
        public async Task<JsonResult> AutoCompleteClubs(string prefix)
        {
            var clubs = (await _apiProvider
                .GetClubsAsync()
                .ConfigureAwait(false))
                .Where(c =>
                    c.Name.Sanitize().Contains(prefix.Sanitize())
                    || c.AllowedNames?.Any(_ => _.Sanitize().Contains(prefix.Sanitize())) == true);

            return Json(clubs.Select(x => x.Name));
        }

        [HttpPost]
        public async Task<JsonResult> AutoCompleteCountries(string prefix)
        {
            var countries = (await _apiProvider
                .GetCountriesAsync()
                .ConfigureAwait(false))
                .Where(c =>
                    c.Value.Sanitize().Contains(prefix.Sanitize()));

            return Json(countries);
        }

        protected IReadOnlyDictionary<ulong, string> GetPositions()
        {
            return Enum
                .GetValues(typeof(Positions))
                .Cast<Positions>()
                .ToDictionary(_ => (ulong)_, _ => _.GetLabel());
        }

        protected (string token, string login) GetAuthenticationCookie()
        {
            var cookieValue = GetCookieContent(_cryptedAuthenticationCookieName);
            if (cookieValue != null)
            {
                var cookieParts = cookieValue.Split(CookiePartsSeparator);
                if (cookieParts.Length > 1)
                {
                    return (cookieParts[0], cookieParts[1]);
                }
            }

            return (null, null);
        }

        protected void SetAuthenticationCookie(string token, string login)
        {
            SetCookie(_cryptedAuthenticationCookieName,
                $"{token}{CookiePartsSeparator}{login}",
                DateTime.Now.AddMonths(1));
        }

        protected void ResetAuthenticationCookie()
        {
            Response.Cookies.Delete(_cryptedAuthenticationCookieName);
        }

        private string GetCookieContent(string cookieName)
        {
            return Request.Cookies.TryGetValue(cookieName, out string cookieValue)
                ? cookieValue.Decrypt()
                : null;
        }

        private void SetCookie(string cookieName, string cookieValue, DateTime expiration)
        {
            Response.Cookies.Delete(cookieName);
            Response.Cookies.Append(
                cookieName,
                cookieValue.Encrypt(),
                    new CookieOptions
                    {
                        Expires = expiration,
                        IsEssential = true,
                        Secure = false
                    });
        }
    }
}
