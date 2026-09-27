using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using KikoleSite.Configuration;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Helpers;
using KikoleSite.Identity;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using KikoleSite.ViewModels.Emails;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KikoleSite.Controllers;

public class AccountController : KikoleBaseController
{
    private readonly IStringLocalizer<AccountController> _localizer;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILookupNormalizer _lookupNormalizer;
    private readonly IEmailProtector _emailProtector;
    private readonly IEmailSender _emailSender;
    private readonly IRazorViewRenderer _emailRenderer;
    private readonly ILogger<AccountController> _logger;
    private readonly RegistrationOptions _registrationOptions;
    private readonly EmailOptions _emailOptions;

    public AccountController(IStringLocalizer<AccountController> localizer,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILookupNormalizer lookupNormalizer,
        IEmailProtector emailProtector,
        IEmailSender emailSender,
        IRazorViewRenderer emailRenderer,
        ILogger<AccountController> logger,
        IOptions<RegistrationOptions> registrationOptions,
        IOptions<EmailOptions> emailOptions,
        IUserRepository userRepository,
        IInternationalService internationalService,
        IClock clock,
        IGameCalendar gameCalendar,
        IPlayerService playerService,
        IBadgeService badgeService,
        IHttpContextAccessor httpContextAccessor)
        : base(userRepository,
            internationalService,
            clock,
            gameCalendar,
            playerService,
            badgeService,
            httpContextAccessor)
    {
        _localizer = localizer;
        _userManager = userManager;
        _signInManager = signInManager;
        _lookupNormalizer = lookupNormalizer;
        _emailProtector = emailProtector;
        _emailSender = emailSender;
        _emailRenderer = emailRenderer;
        _logger = logger;
        _registrationOptions = registrationOptions.Value;
        _emailOptions = emailOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return await RenderIndexAsync(new AccountModel());
    }

    [HttpPost]
    public async Task<IActionResult> LogOut()
    {
        await _signInManager.SignOutAsync();
        // SignOutAsync ne rafraichit pas HttpContext.User pour la reponse en cours
        // (seul le cookie change, pris en compte a la prochaine requete) : sans ca,
        // le menu de _Layout afficherait encore l'utilisateur comme connecte sur
        // cette meme page.
        HttpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        return await RenderIndexAsync(new AccountModel());
    }

    [HttpPost]
    public async Task<IActionResult> LogIn(AccountModel model)
    {
        if (string.IsNullOrWhiteSpace(model.LoginSubmission)
            || string.IsNullOrWhiteSpace(model.PasswordSubmission))
            model.Error = _localizer["InvalidForm"];
        else
        {
            // connexion par login ou par email (standard) : on essaie d'abord le login,
            // moins couteux et cas le plus frequent.
            var user = await _userManager.FindByNameAsync(model.LoginSubmission)
                ?? await _userManager.FindByEmailAsync(model.LoginSubmission);

            if (user == null)
                model.Error = _localizer["InvalidCredentials"];
            else
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user, model.PasswordSubmission, isPersistent: true, lockoutOnFailure: true);

                if (result.IsLockedOut)
                    model.Error = _localizer["AccountLockedOut"];
                else if (result.IsNotAllowed)
                    // RequireConfirmedEmail : refuse la connexion tant que l'adresse n'est pas confirmee.
                    model.Error = _localizer["EmailNotConfirmed"];
                else if (!result.Succeeded)
                    model.Error = _localizer["InvalidCredentials"];
                else
                {
                    await _userRepository.CreateLoginHistoryAsync(
                            user.Id, Request.HttpContext.Connection.RemoteIpAddress?.ToString());

                    return RedirectToAction("Index", "Home");
                }
            }
        }

        return await RenderIndexAsync(model);
    }

    private static bool IsRecoveryForbidden(ApplicationUser user)
    {
        return user.UserType >= UserTypes.Administrator;
    }

    private static bool IsValidEmailFormat(string email)
        => MailAddress.TryCreate(email, out _);

    private bool IsBlockedEmailDomain(string email)
    {
        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0)
            return false;

        var domain = email[(atIndex + 1)..].Trim();
        return _registrationOptions.BlockedEmailDomains
            .Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase));
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var model = new AccountModel();

        var user = string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);

        if (user == null || string.IsNullOrWhiteSpace(token))
            model.Error = _localizer["EmailConfirmationFailed"];
        else
        {
            var confirmation = await _userManager.ConfirmEmailAsync(user, token);
            if (confirmation.Succeeded)
                model.SuccessInfo = _localizer["EmailConfirmed"];
            else
                model.Error = _localizer["EmailConfirmationFailed"];
        }

        return await RenderIndexAsync(model);
    }

    [HttpPost]
    public async Task<IActionResult> RequestPasswordReset(AccountModel model)
    {
        if (string.IsNullOrWhiteSpace(model.PasswordResetEmailSubmission))
            model.Error = _localizer["InvalidForm"];
        else
        {
            // reponse toujours identique, que l'adresse soit connue ou non : evite d'en
            // faire un moyen de deviner les comptes existants.
            model.SuccessInfo = _localizer["PasswordResetRequested"];

            var user = await _userManager.FindByEmailAsync(model.PasswordResetEmailSubmission);
            if (user != null && !IsRecoveryForbidden(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var link = Url.Action("ResetPassword", "Account",
                    new { userId = user.Id, token }, Request.Scheme)!;

                if (_emailOptions.SendingEnabled)
                {
                    var body = await RenderLinkEmailAsync(_localizer["ResetPasswordBody"], link);
                    await _emailSender.SendAsync(user.Email!, _localizer["ResetPasswordSubject"], body);
                }
                else
                    _logger.LogInformation("[Email non envoye - developpement] Lien de reinitialisation pour {UserId} : {Link}", user.Id, link);
            }
        }

        return await RenderIndexAsync(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(ulong userId, string token)
    {
        if (userId == 0 || string.IsNullOrWhiteSpace(token))
            return RedirectToAction("ErrorIndex", "Home");

        return View("ResetPassword", new AccountModel
        {
            ResetPasswordUserId = userId.ToString(),
            ResetPasswordToken = token
        });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(AccountModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ResetPasswordUserId)
            || string.IsNullOrWhiteSpace(model.ResetPasswordToken)
            || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
            || !string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
        {
            model.Error = _localizer["InvalidForm"];
            return View("ResetPassword", model);
        }

        var user = await _userManager.FindByIdAsync(model.ResetPasswordUserId);
        if (user == null)
        {
            model.Error = _localizer["ResetPasswordError"];
            return View("ResetPassword", model);
        }

        var reset = await _userManager.ResetPasswordAsync(user, model.ResetPasswordToken, model.PasswordCreate1Submission);
        if (!reset.Succeeded)
        {
            model.Error = MapPasswordErrorMessage(reset, "ResetPasswordError");
            return View("ResetPassword", model);
        }

        // un mot de passe reinitialise avec succes leve aussi un verrouillage en cours :
        // c'est desormais la seule voie de recuperation, elle ne doit pas rester bloquee.
        await _userManager.ResetAccessFailedCountAsync(user);

        return await RenderIndexAsync(new AccountModel { SuccessInfo = _localizer["PasswordReset"] });
    }

    [HttpPost]
    [Authorization]
    public async Task<IActionResult> ChangeEmail(AccountModel model)
    {
        var user = await _userManager.FindByIdAsync(UserId.ToString());
        if (user == null)
            return RedirectToAction("ErrorIndex", "Home");

        if (string.IsNullOrWhiteSpace(model.PasswordSubmission)
            || string.IsNullOrWhiteSpace(model.NewEmailSubmission)
            || string.IsNullOrWhiteSpace(model.NewEmailConfirmSubmission))
            model.Error = _localizer["InvalidForm"];
        else if (!await _userManager.CheckPasswordAsync(user, model.PasswordSubmission))
            model.Error = _localizer["InvalidPassword"];
        else if (!string.Equals(model.NewEmailSubmission.Trim(), model.NewEmailConfirmSubmission.Trim(), StringComparison.Ordinal))
            model.Error = _localizer["NotMatchingEmail"];
        else if (!IsValidEmailFormat(model.NewEmailSubmission))
            model.Error = _localizer["InvalidEmail"];
        else if (IsBlockedEmailDomain(model.NewEmailSubmission))
            model.Error = _localizer["EmailDomainBlocked"];
        else
        {
            var newEmail = model.NewEmailSubmission.Trim();
            var existing = await _userRepository.GetUserByEmailHashIncludingDisabledAsync(_emailProtector.Hash(newEmail));

            if (existing != null && existing.Id != user.Id)
                model.Error = _localizer["EmailAlreadyUsed"];
            else
            {
                // l'ancienne adresse reste pleinement active tant que le lien n'est pas
                // suivi : rien n'est modifie en base ni en session avant confirmation
                // (le token auto-encode la nouvelle adresse, cf. ChangeEmailAsync).
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                var link = Url.Action("ConfirmEmailChange", "Account",
                    new { userId = user.Id, newEmail, token }, Request.Scheme)!;

                if (_emailOptions.SendingEnabled)
                {
                    var body = await RenderLinkEmailAsync(_localizer["ConfirmEmailChangeBody"], link);
                    await _emailSender.SendAsync(newEmail, _localizer["ConfirmEmailChangeSubject"], body);
                }
                else
                    _logger.LogInformation("[Email non envoye - developpement] Lien de changement d'email pour {UserId} : {Link}", user.Id, link);

                model.SuccessInfo = _localizer["EmailChangeRequested"];
            }
        }

        return await RenderIndexAsync(model);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmailChange(ulong userId, string newEmail, string token)
    {
        var model = new AccountModel();

        var user = userId == 0 ? null : await _userManager.FindByIdAsync(userId.ToString());

        if (user == null || string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(token))
            model.Error = _localizer["EmailConfirmationFailed"];
        else
        {
            var change = await _userManager.ChangeEmailAsync(user, newEmail, token);
            if (change.Succeeded)
                model.SuccessInfo = _localizer["EmailChanged"];
            else
                model.Error = _localizer["EmailConfirmationFailed"];
        }

        return await RenderIndexAsync(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AccountModel model)
    {
        var inviteRequired = _registrationOptions.InviteEnabled;
        var registrationId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(model.LoginCreateSubmission)
            || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
            || string.IsNullOrWhiteSpace(model.EmailCreateSubmission)
            || (inviteRequired && string.IsNullOrWhiteSpace(model.RegistrationId)))
            model.Error = _localizer["InvalidForm"];
        else if (inviteRequired && !Guid.TryParse(model.RegistrationId, out registrationId))
            model.Error = _localizer["InvalidRegistrationGuidFormat"];
        else if (!string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
            model.Error = _localizer["NotMatchingPassword"];
        else if (model.LoginCreateSubmission.Length < 3)
            model.Error = _localizer["TooShortLogin"];
        else if (!IsValidEmailFormat(model.EmailCreateSubmission))
            model.Error = _localizer["InvalidEmail"];
        else if (IsBlockedEmailDomain(model.EmailCreateSubmission))
            model.Error = _localizer["EmailDomainBlocked"];
        else
        {
            var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

            var maxCreationsPerIp = _registrationOptions.MaxCreationsPerIpPerDay;
            var isRateLimited = maxCreationsPerIp.HasValue
                && !string.IsNullOrEmpty(clientIp)
                && !_registrationOptions.RateLimitWhitelistedIps.Contains(clientIp)
                && await _userRepository.GetUserCreationCountSinceAsync(clientIp, _clock.Now.AddDays(-1)) >= maxCreationsPerIp.Value;

            var existingUser = isRateLimited ? null : await _userManager.FindByNameAsync(model.LoginCreateSubmission);

            var emailHash = _emailProtector.Hash(model.EmailCreateSubmission);
            var existingEmail = isRateLimited || existingUser != null
                ? null
                : await _userRepository.GetUserByEmailHashIncludingDisabledAsync(emailHash);

            if (isRateLimited)
                model.Error = _localizer["TooManyAccountsFromThisIp"];
            else if (existingUser != null)
                model.Error = _localizer["AlreadyExistsAccount"];
            else if (existingEmail != null)
                model.Error = _localizer["EmailAlreadyUsed"];
            else
            {
                // hors invitation, rien a verifier avant de creer le compte
                var registration = inviteRequired
                    ? await _userRepository.GetRegistrationGuidAsync(registrationId.ToString())
                    : null;

                if (inviteRequired && registration == null)
                    model.Error = _localizer["InvalidRegistrationId"];
                else if (registration?.UserId.HasValue == true)
                    model.Error = _localizer["UsedRegistrationId"];
                else
                {
                    var sponsorUserId = await ResolveSponsorUserIdAsync(
                            model.SponsorLoginSubmission, model.LoginCreateSubmission);

                    var request = new UserRequest
                    {
                        Login = model.LoginCreateSubmission,
                        Password = model.PasswordCreate1Submission,
                        Email = model.EmailCreateSubmission.Trim(),
                        Ip = clientIp,
                        SponsorUserId = sponsorUserId
                    };

                    var user = request.ToApplicationUser();

                    var creation = await _userManager.CreateAsync(user, request.Password);

                    if (!creation.Succeeded)
                        model.Error = MapPasswordErrorMessage(creation, "UserCreationFailure");
                    else
                    {
                        if (inviteRequired)
                            await _userRepository
                                .LinkRegistrationGuidToUserAsync(registrationId.ToString(), user.Id);

                        if (sponsorUserId.HasValue)
                            await _badgeService
                                .PrepareSponsorshipBadgesAsync(sponsorUserId.Value, ViewHelper.GetLanguage());

                        if (_emailOptions.SendingEnabled)
                        {
                            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            var link = Url.Action("ConfirmEmail", "Account",
                                new { userId = user.Id, token }, Request.Scheme)!;
                            var body = await RenderLinkEmailAsync(_localizer["ConfirmEmailBody"], link);
                            await _emailSender.SendAsync(user.Email!, _localizer["ConfirmEmailSubject"], body);
                        }
                        else
                        {
                            // developpement local : pas d'envoi reel, le compte est
                            // directement considere confirme (auto-inscription).
                            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            await _userManager.ConfirmEmailAsync(user, token);
                        }

                        return await LogIn(new AccountModel
                        {
                            LoginSubmission = model.LoginCreateSubmission,
                            PasswordSubmission = model.PasswordCreate1Submission
                        });
                    }
                }
            }
        }

        return await RenderIndexAsync(model);
    }

    [HttpPost]
    [Authorization]
    public async Task<IActionResult> ChangePassword(AccountModel model)
    {
        var user = await _userManager.FindByIdAsync(UserId.ToString());
        if (user == null)
            return RedirectToAction("ErrorIndex", "Home");

        if (string.IsNullOrWhiteSpace(model.PasswordSubmission)
            || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
            || !string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
            model.Error = _localizer["InvalidForm"];
        else
        {
            var change = await _userManager.ChangePasswordAsync(
                    user, model.PasswordSubmission, model.PasswordCreate1Submission);

            if (!change.Succeeded)
                model.Error = MapPasswordErrorMessage(change, "ResetPasswordError");
            else
                model.SuccessInfo = _localizer["PasswordChanged"];
        }

        return await RenderIndexAsync(model);
    }

    /// <summary>
    /// Resout le login de parrain saisi a l'inscription en identifiant, silencieusement :
    /// aucune des trois raisons de refus (login inconnu, parrain desactive, auto-parrainage)
    /// ne bloque l'inscription ni ne remonte d'erreur - <c>sponsor_user_id</c> reste juste
    /// a <c>null</c>, comme demande.
    /// </summary>
    private async Task<ulong?> ResolveSponsorUserIdAsync(string? sponsorLoginSubmission, string newAccountLogin)
    {
        if (!_registrationOptions.SponsorshipEnabled || string.IsNullOrWhiteSpace(sponsorLoginSubmission))
            return null;

        var sponsor = await _userManager.FindByNameAsync(sponsorLoginSubmission);
        if (sponsor == null || sponsor.IsDisabled)
            return null;

        var normalizedNewLogin = _lookupNormalizer.NormalizeName(newAccountLogin);
        if (sponsor.NormalizedUserName == normalizedNewLogin)
            return null;

        return sponsor.Id;
    }

    private Task<string> RenderLinkEmailAsync(string introduction, string link)
    {
        return _emailRenderer.RenderAsync("/Views/Emails/LinkEmail.cshtml", new LinkEmailModel
        {
            Introduction = introduction,
            Link = link,
            ValidityNotice = _localizer["LinkValidityNotice", _emailOptions.TokenLifetimeHours]
        });
    }

    /// <summary>
    /// Rend la vue Index en refletant l'etat de connexion reel (plutot que de le recopier
    /// a la main a la fin de chaque action, ce qui oublie facilement un cas d'erreur).
    /// </summary>
    private async Task<IActionResult> RenderIndexAsync(AccountModel model)
    {
        model.RegistrationInviteEnabled = _registrationOptions.InviteEnabled;
        model.SponsorshipEnabled = _registrationOptions.SponsorshipEnabled;
        model.IsAuthenticated = UserId > 0;
        model.Login = UserLogin;

        if (UserId > 0)
        {
            var me = await _userManager.FindByIdAsync(UserId.ToString());
            model.Email = me?.Email;

            if (_registrationOptions.SponsorshipEnabled)
            {
                if (me?.SponsorUserId.HasValue == true)
                {
                    var sponsor = await _userRepository.GetUserByIdIncludingDisabledAsync(me.SponsorUserId.Value);
                    model.SponsorLogin = sponsor?.Login;
                }

                var godchildren = await _userRepository.GetGodchildrenAsync(UserId);
                model.Godchildren = godchildren
                    .Select(g => (g.Login, g.IsDisabled))
                    .OrderBy(g => g.Login)
                    .ToList();
            }
        }

        return View("Index", model);
    }

    /// <summary>
    /// Traduit les motifs d'echec connus d'Identity (longueur, mot de passe deja
    /// compromis, ancien mot de passe incorrect) en messages specifiques ; le reste
    /// retombe sur <paramref name="fallbackResourceKey"/>.
    /// </summary>
    private string MapPasswordErrorMessage(IdentityResult result, string fallbackResourceKey)
    {
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordTooShort)))
            return _localizer["TooShortPassword"];

        if (result.Errors.Any(e => e.Code == HibpPasswordValidator.PwnedPasswordErrorCode))
            return _localizer["PasswordCompromised"];

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
            return _localizer["PasswordDoesNotMatch"];

        return _localizer[fallbackResourceKey];
    }
}
