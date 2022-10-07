using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Helpers;
using KikoleSite.Interfaces;
using KikoleSite.Interfaces.Repositories;
using KikoleSite.Interfaces.Services;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public abstract class KikoleBaseController : Controller
    {
        internal const string CryptedAuthenticationCookieName = "AccountFormCrypt";
        internal const string CookiePartsSeparator = "§§§";
        internal const string UserIdItemData = "UserId";
        internal const string UserLoginItemData = "UserLogin";
        internal const string UserTypeItemData = "UserType";

        protected ulong UserId => _httpContextAccessor.HttpContext.Items.ContainsKey(UserIdItemData)
            ? Convert.ToUInt64(_httpContextAccessor.HttpContext.Items[UserIdItemData])
            : 0;

        protected string UserLogin => _httpContextAccessor.HttpContext.Items.ContainsKey(UserLoginItemData)
            ? _httpContextAccessor.HttpContext.Items[UserLoginItemData]?.ToString()
            : null;

        protected UserTypes UserType => _httpContextAccessor.HttpContext.Items.ContainsKey(UserTypeItemData)
            ? Enum.Parse<UserTypes>(_httpContextAccessor.HttpContext.Items[UserTypeItemData].ToString())
            : UserTypes.StandardUser;

        private static ConcurrentDictionary<string, IReadOnlyDictionary<ulong, string>> _countriesCache;
        private static ProposalChart _proposalChartCache;
        private static IReadOnlyCollection<Club> _clubsCache;

        private readonly IInternationalRepository _internationalRepository;

        protected readonly IUserRepository _userRepository;
        protected readonly ICrypter _crypter;
        protected readonly IClock _clock;
        protected readonly IPlayerService _playerService;
        protected readonly IClubRepository _clubRepository;
        protected readonly IBadgeService _badgeService;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected KikoleBaseController(IUserRepository userRepository,
            ICrypter crypter,
            IInternationalRepository internationalRepository,
            IClock clock,
            IPlayerService playerService,
            IClubRepository clubRepository,
            IBadgeService badgeService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _crypter = crypter;
            _internationalRepository = internationalRepository;
            _clock = clock;
            _playerService = playerService;
            _clubRepository = clubRepository;
            _badgeService = badgeService;
            _httpContextAccessor = httpContextAccessor;
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

        protected string GetSubmitAction()
        {
            var submitKeys = _httpContextAccessor.HttpContext.Request.Form.Keys.Where(x => x.StartsWith("submit-"));

            if (submitKeys.Count() != 1)
                return null;

            var submitKeySplit = submitKeys.First().Split('-');
            if (submitKeySplit.Length != 2)
                return null;

            return submitKeySplit[1];
        }

        protected IReadOnlyDictionary<ulong, string> GetPositions()
        {
            return Enum
                .GetValues(typeof(Positions))
                .Cast<Positions>()
                .ToDictionary(_ => (ulong)_, _ => _.GetLabel());
        }

        protected void ResetAuthenticationCookie()
        {
            Response.Cookies.Delete(CryptedAuthenticationCookieName);
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
                var lng = ViewHelper.GetLanguage();

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

        protected void SetAuthenticationCookie(string token, string login)
        {
            SetCookie(CryptedAuthenticationCookieName,
                $"{token}{CookiePartsSeparator}{login}",
                _clock.Now.AddMonths(1));
        }

        protected bool IsTypeOfUser(UserTypes minimalType)
        {
            return (ulong)UserType >= (ulong)minimalType;
        }

        private void SetCookie(string cookieName, string cookieValue, DateTime expiration)
        {
            Response.Cookies.Delete(cookieName);
            Response.Cookies.Append(
                cookieName,
                _crypter.EncryptCookie(cookieValue),
                    new CookieOptions
                    {
                        Expires = expiration,
                        IsEssential = true,
                        Secure = false
                    });
        }
    }
}
