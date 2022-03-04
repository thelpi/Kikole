using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public abstract class KikoleBaseController : ControllerBase
    {
        const string AuthToken = "AuthToken";

        private readonly IHttpContextAccessor _httpContextAccessor;

        protected KikoleBaseController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected bool IsAdminAuthentification()
        {
            return true;
        }

        protected bool IsFaultedAuthentication()
        {
            return false;
        }

        protected ulong? GetAuthenticatedUser()
        {
            return null;
        }

        private string GetAuthTokenValue()
        {
            if (!_httpContextAccessor.HttpContext.Request.Headers.ContainsKey(AuthToken))
                return null;

            var values = _httpContextAccessor.HttpContext.Request.Headers[AuthToken];

            if (values.Count != 1)
                return null;

            return string.IsNullOrWhiteSpace(values[0])
                ? null
                : values[0].Trim();
        }
    }
}
