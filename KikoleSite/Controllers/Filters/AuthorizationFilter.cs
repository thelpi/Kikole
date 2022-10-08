using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace KikoleSite.Controllers.Filters
{
    public class AuthorizationFilter : IAsyncAuthorizationFilter
    {
        private const int DelayBetweenUserChecks = 10;

        private readonly IUserRepository _userRepository;
        private readonly ICrypter _crypter;
        private readonly IClock _clock;

        private static readonly ConcurrentDictionary<ulong, (DateTime expirationDate, UserDto userData)> _usersCheckCache
            = new ConcurrentDictionary<ulong, (DateTime, UserDto)>();

        public AuthorizationFilter(
            IUserRepository userRepository,
            ICrypter crypter,
            IClock clock)
        {
            _userRepository = userRepository;
            _crypter = crypter;
            _clock = clock;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var (token, login) = GetAuthenticationCookie(context.HttpContext.Request);
            var userId = await ExtractUserIdFromTokenAsync(token).ConfigureAwait(false);
            var userType = _usersCheckCache.ContainsKey(userId)
                ? (UserTypes)_usersCheckCache[userId].userData.UserTypeId
                : UserTypes.StandardUser;

            if ((context.ActionDescriptor as ControllerActionDescriptor)
                .MethodInfo
                .GetCustomAttributes(typeof(AuthorizationAttribute), true)
                .FirstOrDefault() is AuthorizationAttribute authorizationAttribute)
            {
                if (userId == 0 || !IsTypeOfUser(userId, authorizationAttribute.MinimalUserType))
                {
                    context.Result = new RedirectToRouteResult(
                        new RouteValueDictionary(
                            new
                            {
                                action = nameof(HomeController.ErrorIndex),
                                controller = "Home"
                            }));
                }
            }

            context.HttpContext.Items.Add(KikoleBaseController.UserIdItemData, userId);
            context.HttpContext.Items.Add(KikoleBaseController.UserLoginItemData, login);
            context.HttpContext.Items.Add(KikoleBaseController.UserTypeItemData, userType);
        }

        private bool IsTypeOfUser(ulong userId, UserTypes minimalType)
        {
            return _usersCheckCache[userId].userData.UserTypeId >= (ulong)minimalType;
        }

        private async Task<ulong> ExtractUserIdFromTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return 0;

            var tokenParts = token.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (tokenParts.Length != 3
                || !ulong.TryParse(tokenParts[0], out var userId)
                || !ulong.TryParse(tokenParts[1], out var userTypeId)
                || !Enum.GetValues(typeof(UserTypes)).Cast<UserTypes>().Any(_ => (ulong)_ == userTypeId))
                return 0;

            if (!_crypter.Encrypt($"{userId}_{userTypeId}").Equals(tokenParts[2]))
                return 0;

            if (!_usersCheckCache.ContainsKey(userId) || (_clock.Now - _usersCheckCache[userId].expirationDate).TotalMinutes > DelayBetweenUserChecks)
            {
                var user = await _userRepository
                    .GetUserByIdAsync(userId)
                    .ConfigureAwait(false);

                if (user == null)
                    return 0;
                else
                    _usersCheckCache.AddOrUpdate(userId, (_clock.Now, user), (k, v) => v);
            }

            return userId;
        }

        private (string token, string login) GetAuthenticationCookie(HttpRequest request)
        {
            var cookieValue = request.Cookies.TryGetValue(KikoleBaseController.CryptedAuthenticationCookieName, out string cookieValueTmp)
                ? _crypter.DecryptCookie(cookieValueTmp)
                : null;
            if (cookieValue != null)
            {
                var cookieParts = cookieValue.Split(KikoleBaseController.CookiePartsSeparator);
                if (cookieParts.Length > 1)
                {
                    return (cookieParts[0], cookieParts[1]);
                }
            }

            return (null, null);
        }
    }
}
