using System;
using KikoleApi.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Controllers
{
    public abstract class KikoleBaseController : ControllerBase
    {
        const string AuthTokenHeader = "AuthToken";

        private readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly ICrypter Crypter;
        private readonly bool _disableAuthorization;

        protected KikoleBaseController(IHttpContextAccessor httpContextAccessor,
            ICrypter crypter,
            IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            Crypter = crypter;
            _disableAuthorization = configuration.GetValue<bool>("DisableAuthorization");
        }

        protected bool IsAdminAuthentification()
        {
            if (_disableAuthorization)
                return true;

            var (_, isAdmin, isFaulted) = ExtractAuthTokenHeaderInfo();

            return !isFaulted && isAdmin;
        }

        protected bool IsFaultedAuthentication()
        {
            if (_disableAuthorization)
                return false;

            return ExtractAuthTokenHeaderInfo().isFaulted;
        }

        protected ulong? GetAuthenticatedUser()
        {
            if (_disableAuthorization)
                return null;

            return ExtractAuthTokenHeaderInfo().id;
        }

        private string GetAuthTokenHeaderValue()
        {
            if (!_httpContextAccessor.HttpContext.Request.Headers.ContainsKey(AuthTokenHeader))
                return null;

            var values = _httpContextAccessor.HttpContext.Request.Headers[AuthTokenHeader];

            if (values.Count != 1)
                return null;

            return string.IsNullOrWhiteSpace(values[0])
                ? null
                : values[0].Trim();
        }

        private (ulong? id, bool isAdmin, bool isFaulted) ExtractAuthTokenHeaderInfo()
        {
            var token = GetAuthTokenHeaderValue();

            if (string.IsNullOrWhiteSpace(token))
                return (null, false, false);

            var tokenParts = token.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (tokenParts.Length != 3
                || !ulong.TryParse(tokenParts[0], out var userId)
                || !byte.TryParse(tokenParts[1], out var isAdmin))
                return (null, false, true);

            return (userId, isAdmin > 0, !Crypter.Encrypt($"{userId}_{isAdmin}").Equals(tokenParts[2]));
        }
    }
}
