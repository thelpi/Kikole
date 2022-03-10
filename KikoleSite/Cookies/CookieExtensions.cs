using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace KikoleSite.Cookies
{
    internal static class CookieExtensions
    {
        const string AuthenticationCookieName = "AccountForm";
        const string SubmissionFormCookieName = "SubmissionForm";

        const string CookiePartsSeparator = "§§§";

        internal static void SetCookie(this Controller controller,
            string cookieName,
            string cookieValue,
            DateTime expiration)
        {
            controller.Response.Cookies.Delete(cookieName);
            var option = new CookieOptions
            {
                Expires = expiration,
                IsEssential = true,
                Secure = false
            };
            controller.Response.Cookies.Append(cookieName, cookieValue, option);
        }

        internal static (string token, string login) GetAuthenticationCookie(this Controller controller)
        {
            if (controller.Request.Cookies.TryGetValue(AuthenticationCookieName, out string cookieValue))
            {
                var cookieParts = cookieValue.Split(CookiePartsSeparator);
                if (cookieParts.Length > 1)
                {
                    return (cookieParts[0], cookieParts[1]);
                }
            }

            return (null, null);
        }

        internal static SubmissionForm GetSubmissionFormCookie(this Controller controller)
        {
            if (controller.Request.Cookies.TryGetValue(SubmissionFormCookieName, out string cookieValue))
            {
                try
                {
                    return JsonConvert.DeserializeObject<SubmissionForm>(cookieValue);
                }
                catch { }
            }

            return null;
        }

        internal static void SetSubmissionFormCookie(this Controller controller, SubmissionForm value)
        {
            controller.SetCookie(SubmissionFormCookieName,
                JsonConvert.SerializeObject(value),
                DateTime.Now.AddDays(1).Date);
        }

        internal static void SetCookie(this Controller controller,
            string token,
            string login)
        {
            controller.SetCookie(AuthenticationCookieName,
                $"{token}{CookiePartsSeparator}{login}",
                DateTime.Now.AddMonths(1));
        }

        internal static void ResetAuthenticationCookie(this Controller controller)
        {
            controller.Response.Cookies.Delete(AuthenticationCookieName);
        }
    }
}
