using System;
using System.Linq;
using System.Net;
using KikoleApi.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Controllers.Filters
{
    public class AuthorizationFilter : IAuthorizationFilter
    {
        const string AuthTokenHeader = "AuthToken";
        const string UserIdQueryParam = "userId";

        private readonly ICrypter _crypter;
        private readonly bool _disableAuthorization;

        public AuthorizationFilter(ICrypter crypter,
            IConfiguration configuration)
        {
            _crypter = crypter;
            _disableAuthorization = configuration.GetValue<bool>("DisableAuthorization");
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (_disableAuthorization)
                return;

            if (!(context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor))
                return;

            var authAttribute = controllerActionDescriptor.MethodInfo
                .GetCustomAttributes(true)
                .FirstOrDefault(aa => aa is AuthenticationLevelAttribute);

            if (authAttribute == null)
                return;

            var (id, isAdmin, isFaulted) = ExtractAuthTokenHeaderInfo(context.HttpContext);

            if (!IsAuthorized((authAttribute as AuthenticationLevelAttribute).Level, id, isAdmin, isFaulted))
                context.Result = new StatusCodeResult((int)HttpStatusCode.Unauthorized);
            else
                context.HttpContext.Request.QueryString = context.HttpContext.Request.QueryString.Add(UserIdQueryParam, id.GetValueOrDefault(0).ToString());
        }

        private static string GetAuthTokenHeaderValue(HttpContext httpContext)
        {
            if (!httpContext.Request.Headers.ContainsKey(AuthTokenHeader))
                return null;

            var values = httpContext.Request.Headers[AuthTokenHeader];

            if (values.Count != 1)
                return null;

            return string.IsNullOrWhiteSpace(values[0])
                ? null
                : values[0].Trim();
        }

        private (ulong? id, bool isAdmin, bool isFaulted) ExtractAuthTokenHeaderInfo(HttpContext httpContext)
        {
            var token = GetAuthTokenHeaderValue(httpContext);

            if (string.IsNullOrWhiteSpace(token))
                return (null, false, false);

            var tokenParts = token.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (tokenParts.Length != 3
                || !ulong.TryParse(tokenParts[0], out var userId)
                || !byte.TryParse(tokenParts[1], out var isAdmin))
                return (null, false, true);

            return (userId, isAdmin > 0, !_crypter.Encrypt($"{userId}_{isAdmin}").Equals(tokenParts[2]));
        }

        private static bool IsAuthorized(AuthenticationLevel level,
            ulong? id, bool isAdmin, bool isFaulted)
        {
            if (level == AuthenticationLevel.None)
                return true;

            return !isFaulted
                && id.HasValue
                && (isAdmin || level != AuthenticationLevel.AdminAuthenticated);
        }
    }
}
