using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Helpers;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers;

public abstract class KikoleBaseController : Controller
{
    internal const string CryptedAuthenticationCookieName = "AccountFormCrypt";
    internal const string CookiePartsSeparator = "§§§";
    internal const string UserIdItemData = "UserId";
    internal const string UserLoginItemData = "UserLogin";
    internal const string UserTypeItemData = "UserType";

    /// <summary>
    /// Contexte de la requete en cours. Ces membres ne sont lus que depuis une action,
    /// ou le contexte est toujours present : son absence est un defaut de programmation.
    /// </summary>
    private HttpContext HttpContextEnsured => _httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("Aucun contexte HTTP : ce membre n'est utilisable que pendant une requete.");

    protected ulong UserId => HttpContextEnsured.Items.TryGetValue(UserIdItemData, out var userId)
        ? Convert.ToUInt64(userId)
        : 0;

    protected string? UserLogin => HttpContextEnsured.Items.TryGetValue(UserLoginItemData, out var userLogin)
        ? userLogin?.ToString()
        : null;

    // a defaut de type exploitable, on retombe sur le profil le moins privilegie
    protected UserTypes UserType => HttpContextEnsured.Items.TryGetValue(UserTypeItemData, out var userType)
            && Enum.TryParse<UserTypes>(userType?.ToString(), out var parsedUserType)
        ? parsedUserType
        : UserTypes.StandardUser;

    protected readonly IUserRepository _userRepository;
    protected readonly ICrypter _crypter;
    protected readonly IClock _clock;
    protected readonly IPlayerService _playerService;
    protected readonly IClubRepository _clubRepository;
    protected readonly IBadgeService _badgeService;
    protected readonly IInternationalService _internationalService;
    protected readonly IHttpContextAccessor _httpContextAccessor;

    protected KikoleBaseController(IUserRepository userRepository,
        ICrypter crypter,
        IInternationalService internationalService,
        IClock clock,
        IPlayerService playerService,
        IClubRepository clubRepository,
        IBadgeService badgeService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _crypter = crypter;
        _internationalService = internationalService;
        _clock = clock;
        _playerService = playerService;
        _clubRepository = clubRepository;
        _badgeService = badgeService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    public async Task<JsonResult> AutoCompleteClubs(string prefix)
    {
        var clubs = (await GetClubsAsync())
            .Where(c =>
                c.Name.Sanitize().Contains(prefix.Sanitize())
                || c.AllowedNames?.Any(_ => _.Sanitize().Contains(prefix.Sanitize())) == true);

        return Json(clubs.Select(x => x.Name));
    }

    [HttpPost]
    public async Task<JsonResult> AutoCompleteContinents(string prefix)
    {
        var continents = (await GetContinentsAsync())
            .Where(c =>
                c.Value.Sanitize().Contains(prefix.Sanitize()));

        return Json(continents);
    }

    [HttpPost]
    public async Task<JsonResult> AutoCompleteCountries(string prefix)
    {
        var countries = (await GetCountriesAsync())
            .Where(c =>
                c.Value.Sanitize().Contains(prefix.Sanitize()));

        return Json(countries);
    }

    protected string? GetSubmitAction()
    {
        var submitKeys = HttpContextEnsured.Request.Form.Keys.Where(x => x.StartsWith("submit-"));

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

    // Raccourcis vers le referentiel : ils resolvent la langue depuis la culture de la
    // requete, ce que le service ne fait pas — c'est ce qui le rend testable.

    protected Task<IReadOnlyCollection<Club>> GetClubsAsync()
    {
        return _internationalService.GetClubsAsync();
    }

    protected Task<IReadOnlyDictionary<ulong, string>> GetCountriesAsync()
    {
        return _internationalService.GetCountriesAsync(ViewHelper.GetLanguage());
    }

    protected Task<IReadOnlyDictionary<ulong, string>> GetContinentsAsync()
    {
        return _internationalService.GetContinentsAsync(ViewHelper.GetLanguage());
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
