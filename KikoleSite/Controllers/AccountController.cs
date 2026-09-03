using System;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Configuration;
using KikoleSite.Identity;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace KikoleSite.Controllers;

public class AccountController : KikoleBaseController
{
    private readonly IStringLocalizer<AccountController> _localizer;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly RegistrationOptions _registrationOptions;

    public AccountController(IStringLocalizer<AccountController> localizer,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IOptions<RegistrationOptions> registrationOptions,
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
        _passwordHasher = passwordHasher;
        _registrationOptions = registrationOptions.Value;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new AccountModel
        {
            IsAuthenticated = UserId > 0,
            Login = UserLogin,
            RegistrationInviteEnabled = _registrationOptions.InviteEnabled
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(AccountModel model)
    {
        model.RegistrationInviteEnabled = _registrationOptions.InviteEnabled;

        var submitFrom = GetSubmitAction();

        if (submitFrom == "logoff")
        {
            await _signInManager.SignOutAsync();
            model = new AccountModel();
        }
        else if (submitFrom == "login" || model.ForceLoginAction)
        {
            if (string.IsNullOrWhiteSpace(model.LoginSubmission)
                || string.IsNullOrWhiteSpace(model.PasswordSubmission))
                model.Error = _localizer["InvalidForm"];
            else
            {
                var user = await _userManager.FindByNameAsync(model.LoginSubmission);

                if (user == null)
                    model.Error = _localizer["InvalidCredentials"];
                else
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        user, model.PasswordSubmission, isPersistent: true, lockoutOnFailure: true);

                    if (result.IsLockedOut)
                        model.Error = _localizer["AccountLockedOut"];
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
        }
        else if (submitFrom == "getloginquestion")
        {
            if (string.IsNullOrWhiteSpace(model.LoginRecoverySubmission))
                model.Error = _localizer["InvalidForm"];
            else
            {
                // get question from login
                var user = await _userManager.FindByNameAsync(model.LoginRecoverySubmission);

                if (user != null)
                    model.QuestionRecovery = user.PasswordResetQuestion;
                else
                    model.Error = _localizer["UserDoesNotExist"];
            }
        }
        else if (submitFrom == "resetpassword")
        {
            if (string.IsNullOrWhiteSpace(model.LoginRecoverySubmission)
                || string.IsNullOrWhiteSpace(model.RecoveryACreate)
                || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                || !string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                model.Error = _localizer["InvalidForm"];
            else
            {
                var user = await _userManager.FindByNameAsync(model.LoginRecoverySubmission);

                if (user == null)
                    model.Error = _localizer["ResetPasswordError"];
                else if (await _userManager.IsLockedOutAsync(user))
                    model.Error = _localizer["AccountLockedOut"];
                else
                {
                    // la reponse de securite est un secret bien plus devinable qu'un mot de
                    // passe : elle passe par le meme compteur de verrouillage que la connexion,
                    // sinon elle deviendrait le maillon faible.
                    var verification = _passwordHasher.VerifyHashedPassword(
                        user, user.PasswordResetAnswerHash, model.RecoveryACreate);

                    if (verification == PasswordVerificationResult.Failed)
                    {
                        await _userManager.AccessFailedAsync(user);
                        model.Error = _localizer["ResetPasswordError"];
                    }
                    else
                    {
                        await _userManager.ResetAccessFailedCountAsync(user);

                        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
                            user.PasswordResetAnswerHash = _passwordHasher.HashPassword(user, model.RecoveryACreate);

                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var reset = await _userManager.ResetPasswordAsync(user, token, model.PasswordCreate1Submission);

                        if (!reset.Succeeded)
                            model.Error = MapPasswordErrorMessage(reset, "ResetPasswordError");
                        else
                            model.SuccessInfo = _localizer["PasswordReset"];
                    }
                }
            }
        }
        else if (submitFrom == "resetqanda")
        {
            if (UserId == 0)
                return RedirectToAction("ErrorIndex", "Home");

            if (string.IsNullOrWhiteSpace(model.RecoveryQCreate)
                || string.IsNullOrWhiteSpace(model.RecoveryACreate))
                model.Error = _localizer["InvalidForm"];
            else
            {
                var user = await _userManager.FindByIdAsync(UserId.ToString());
                if (user == null)
                    return RedirectToAction("ErrorIndex", "Home");

                user.PasswordResetQuestion = model.RecoveryQCreate;
                user.PasswordResetAnswerHash = _passwordHasher.HashPassword(user, model.RecoveryACreate);

                await _userManager.UpdateAsync(user);

                model.SuccessInfo = _localizer["QandAUpdated"];
                model.IsAuthenticated = true;
                model.Login = UserLogin;
            }
        }
        else if (submitFrom == "create")
        {
            var inviteRequired = _registrationOptions.InviteEnabled;
            var registrationId = Guid.Empty;

            if (string.IsNullOrWhiteSpace(model.LoginCreateSubmission)
                || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                || (inviteRequired && string.IsNullOrWhiteSpace(model.RegistrationId)))
                model.Error = _localizer["InvalidForm"];
            else if (inviteRequired && !Guid.TryParse(model.RegistrationId, out registrationId))
                model.Error = _localizer["InvalidRegistrationGuidFormat"];
            else if (!string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                model.Error = _localizer["NotMatchingPassword"];
            else if (model.LoginCreateSubmission.Length < 3)
                model.Error = _localizer["TooShortLogin"];
            else
            {
                var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

                var maxCreationsPerIp = _registrationOptions.MaxCreationsPerIpPerDay;
                var isRateLimited = maxCreationsPerIp.HasValue
                    && !string.IsNullOrEmpty(clientIp)
                    && !_registrationOptions.RateLimitWhitelistedIps.Contains(clientIp)
                    && await _userRepository.GetUserCreationCountSinceAsync(clientIp, _clock.Now.AddDays(-1)) >= maxCreationsPerIp.Value;

                var existingUser = isRateLimited ? null : await _userManager.FindByNameAsync(model.LoginCreateSubmission);

                if (isRateLimited)
                    model.Error = _localizer["TooManyAccountsFromThisIp"];
                else if (existingUser != null)
                    model.Error = _localizer["AlreadyExistsAccount"];
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
                        var request = new UserRequest
                        {
                            Login = model.LoginCreateSubmission,
                            Password = model.PasswordCreate1Submission,
                            PasswordResetQuestion = model.RecoveryQCreate,
                            PasswordResetAnswer = model.RecoveryACreate?.Trim(),
                            Ip = clientIp
                        };

                        var (user, rawPasswordResetAnswer) = request.ToApplicationUser();
                        user.PasswordResetAnswerHash = _passwordHasher.HashPassword(user, rawPasswordResetAnswer);

                        var creation = await _userManager.CreateAsync(user, request.Password);

                        if (!creation.Succeeded)
                            model.Error = MapPasswordErrorMessage(creation, "UserCreationFailure");
                        else
                        {
                            if (inviteRequired)
                                await _userRepository
                                    .LinkRegistrationGuidToUserAsync(registrationId.ToString(), user.Id);

                            return await Index(new AccountModel
                            {
                                LoginSubmission = model.LoginCreateSubmission,
                                PasswordSubmission = model.PasswordCreate1Submission,
                                ForceLoginAction = true
                            });
                        }
                    }
                }
            }
        }
        else if (submitFrom == "changepassword")
        {
            if (UserId == 0)
                return RedirectToAction("ErrorIndex", "Home");

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
                {
                    model.IsAuthenticated = true;
                    model.Login = UserLogin;
                    model.SuccessInfo = _localizer["PasswordChanged"];
                }
            }
        }

        return View(model);
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
