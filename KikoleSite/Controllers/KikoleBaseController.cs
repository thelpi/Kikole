using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Interfaces.Services;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers
{
    public abstract class KikoleBaseController : Controller
    {
        internal const string _cryptedAuthenticationCookieName = "AccountFormCrypt";
        internal const string CookiePartsSeparator = "§§§";

        // minutes
        private const int DelayBetweenUserChecks = 10;

        private static ConcurrentDictionary<string, IReadOnlyDictionary<ulong, string>> _countriesCache;
        private static ProposalChart _proposalChartCache;
        private static IReadOnlyCollection<Club> _clubsCache;

        private readonly ConcurrentDictionary<ulong, DateTime> _usersCheckCache;
        protected readonly IUserRepository _userRepository;
        protected readonly ICrypter _crypter;
        protected readonly IStringLocalizer<Translations> _resources;
        private readonly IInternationalRepository _internationalRepository;
        protected readonly IMessageRepository _messageRepository;
        protected readonly IClock _clock;
        protected readonly IPlayerService _playerService;
        protected readonly IClubRepository _clubRepository;
        protected readonly IProposalService _proposalService;
        protected readonly IBadgeService _badgeService;
        protected readonly ILeaderService _leaderService;
        protected readonly IStatisticService _statisticService;
        protected readonly IDiscussionRepository _discussionRepository;

        protected KikoleBaseController(IUserRepository userRepository,
            ICrypter crypter,
            IStringLocalizer<Translations> resources,
            IInternationalRepository internationalRepository,
            IMessageRepository messageRepository,
            IClock clock,
            IPlayerService playerService,
            IClubRepository clubRepository,
            IProposalService proposalService,
            IBadgeService badgeService,
            ILeaderService leaderService,
            IStatisticService statisticService,
            IDiscussionRepository discussionRepository)
        {
            _userRepository = userRepository;
            _crypter = crypter;
            _resources = resources;
            _internationalRepository = internationalRepository;
            _messageRepository = messageRepository;
            _clock = clock;
            _playerService = playerService;
            _clubRepository = clubRepository;
            _proposalService = proposalService;
            _badgeService = badgeService;
            _leaderService = leaderService;
            _statisticService = statisticService;
            _discussionRepository = discussionRepository;
            _usersCheckCache = new ConcurrentDictionary<ulong, DateTime>();
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
            var clubs = (await GetClubsAsync().ConfigureAwait(false))
                .Where(c =>
                    c.Name.Sanitize().Contains(prefix.Sanitize())
                    || c.AllowedNames?.Any(_ => _.Sanitize().Contains(prefix.Sanitize())) == true);

            return Json(clubs.Select(x => x.Name));
        }

        [HttpPost]
        public async Task<JsonResult> AutoCompleteCountries(string prefix)
        {
            var countries = (await GetCountriesAsync().ConfigureAwait(false))
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

        protected async Task<bool> IsTypeOfUserAsync(string authToken, UserTypes minimalType)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return false;

            return user.UserTypeId >= (ulong)minimalType;
        }

        protected async Task<ulong> ExtractUserIdFromTokenAsync(string token)
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

            if (!_usersCheckCache.ContainsKey(userId) || (_clock.Now - _usersCheckCache[userId]).TotalMinutes > DelayBetweenUserChecks)
            {
                var user = await _userRepository.GetUserByIdAsync(userId).ConfigureAwait(false);
                if (user == null)
                    return 0;
                else
                    _usersCheckCache.AddOrUpdate(userId, _clock.Now, (k, v) => _clock.Now);
            }

            return userId;
        }

        protected async Task<IReadOnlyCollection<Club>> GetClubsAsync(bool resetCache = false)
        {
            if (_clubsCache == null || resetCache)
            {
                var clubs = await _clubRepository
                    .GetClubsAsync()
                    .ConfigureAwait(false);

                _clubsCache = clubs.Select(c => new Club(c)).OrderBy(c => c.Name).ToList();
            }

            return _clubsCache;
        }

        protected async Task<IReadOnlyDictionary<ulong, string>> GetCountriesAsync()
        {
            if (_countriesCache?.ContainsKey(CultureInfo.CurrentCulture.TwoLetterISOLanguageName) != true)
            {
                var lng = Helper.GetLanguage();

                var countries = await _internationalRepository
                    .GetCountriesAsync((ulong)lng)
                    .ConfigureAwait(false);

                var apiCountries = countries.Select(c => new Country(c)).OrderBy(c => c.Name).ToList();

                var dict = apiCountries.ToDictionary(ac => (ulong)ac.Code, ac => ac.Name);

                _countriesCache ??= new ConcurrentDictionary<string, IReadOnlyDictionary<ulong, string>>();
                _countriesCache.AddOrUpdate(
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    dict,
                    (k, v) => dict);
            }

            return _countriesCache[CultureInfo.CurrentCulture.TwoLetterISOLanguageName];
        }

        protected async Task<ProposalChart> GetProposalChartAsync()
        {
            if (_proposalChartCache == null)
            {
                ProposalChart.Default.FirstDate = await _playerService
                    .GetFirstSubmittedPlayerDateAsync(false)
                    .ConfigureAwait(false);

                _proposalChartCache = ProposalChart.Default;
            }

            return _proposalChartCache;
        }

        protected async Task<bool> IsPowerUserAsync(string authToken)
        {
            return await IsTypeOfUserAsync(
                    authToken, UserTypes.PowerUser)
                .ConfigureAwait(false);
        }

        protected async Task<bool> IsAdminUserAsync(string authToken)
        {
            return await IsTypeOfUserAsync(
                    authToken, UserTypes.Administrator)
                .ConfigureAwait(false);
        }

        protected async Task<IReadOnlyCollection<string>> GetUserKnownPlayersAsync(string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            return await _playerService
                .GetKnownPlayerNamesAsync(userId)
                .ConfigureAwait(false);
        }
    }
}
