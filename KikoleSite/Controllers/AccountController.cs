using System;
using System.Threading.Tasks;
using KikoleSite.Helpers;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers;

public class AccountController : KikoleBaseController
{
    private readonly IStringLocalizer<AccountController> _localizer;

    public AccountController(IStringLocalizer<AccountController> localizer,
        IUserRepository userRepository,
        ICrypter crypter,
        IInternationalService internationalService,
        IClock clock,
        IGameCalendar gameCalendar,
        IPlayerService playerService,
        IBadgeService badgeService,
        IHttpContextAccessor httpContextAccessor)
        : base(userRepository,
            crypter,
            internationalService,
            clock,
            gameCalendar,
            playerService,
            badgeService,
            httpContextAccessor)
    {
        _localizer = localizer;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new AccountModel
        {
            IsAuthenticated = UserId > 0,
            Login = UserLogin
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(AccountModel model)
    {
        var submitFrom = GetSubmitAction();

        if (submitFrom == "logoff")
        {
            ResetAuthenticationCookie();
            model = new AccountModel();
        }
        else if (submitFrom == "login" || model.ForceLoginAction)
        {
            if (string.IsNullOrWhiteSpace(model.LoginSubmission)
                || string.IsNullOrWhiteSpace(model.PasswordSubmission))
                model.Error = _localizer["InvalidForm"];
            else
            {
                var existingUser = await _userRepository
                    .GetUserByLoginAsync(model.LoginSubmission.Sanitize());

                if (existingUser == null)
                    model.Error = _localizer["UserDoesNotExist"];
                else if (!_crypter.Encrypt(model.PasswordSubmission).Equals(existingUser.Password))
                    model.Error = _localizer["PasswordDoesNotMatch"];
                else
                {
                    var value = $"{existingUser.Id}_{existingUser.UserTypeId}";

                    var token = $"{value}_{_crypter.Encrypt(value)}";

                    SetAuthenticationCookie(token, model.LoginSubmission);
                    return RedirectToAction("Index", "Home");
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
                var user = await _userRepository
                    .GetUserByLoginAsync(model.LoginRecoverySubmission);

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
                var response = await _userRepository
                    .ResetUserUnknownPasswordAsync(
                        model.LoginRecoverySubmission,
                        _crypter.Encrypt(model.RecoveryACreate),
                        _crypter.Encrypt(model.PasswordCreate1Submission));

                if (response)
                    model.Error = _localizer["ResetPasswordError"];
                else
                    model.SuccessInfo = _localizer["PasswordReset"];
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
                await _userRepository
                    .ResetUserQAndAAsync(UserId, model.RecoveryQCreate, _crypter.Encrypt(model.RecoveryACreate));

                model.SuccessInfo = _localizer["QandAUpdated"];
                model.IsAuthenticated = true;
                model.Login = UserLogin;
            }
        }
        else if (submitFrom == "create")
        {
            if (string.IsNullOrWhiteSpace(model.LoginCreateSubmission)
                || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                || string.IsNullOrWhiteSpace(model.RegistrationId))
                model.Error = _localizer["InvalidForm"];
            else if (!Guid.TryParse(model.RegistrationId, out var registrationId))
                model.Error = _localizer["InvalidRegistrationGuidFormat"];
            else if (!string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                model.Error = _localizer["NotMatchingPassword"];
            else if (model.PasswordCreate1Submission.Length < 6)
                model.Error = _localizer["TooShortPassword"];
            else if (model.LoginCreateSubmission.Length < 3)
                model.Error = _localizer["TooShortLogin"];
            else
            {
                var existingUser = await _userRepository
                    .GetUserByLoginAsync(model.LoginCreateSubmission.Sanitize());

                if (existingUser != null)
                    model.Error = _localizer["AlreadyExistsAccount"];
                else
                {
                    var registration = await _userRepository
                        .GetRegistrationGuidAsync(registrationId.ToString());

                    if (registration == null)
                        model.Error = _localizer["InvalidRegistrationId"];
                    else if (registration.UserId.HasValue)
                        model.Error = _localizer["UsedRegistrationId"];
                    else
                    {
                        var request = new UserRequest
                        {
                            Login = model.LoginCreateSubmission,
                            Password = model.PasswordCreate1Submission,
                            PasswordResetQuestion = model.RecoveryQCreate,
                            PasswordResetAnswer = model.RecoveryACreate?.Trim(),
                            Ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                        };

                        var userId = await _userRepository
                            .CreateUserAsync(request.ToDto(_crypter));

                        if (userId == 0)
                            model.Error = _localizer["UserCreationFailure"];
                        else
                        {
                            await _userRepository
                                .LinkRegistrationGuidToUserAsync(registrationId.ToString(), userId);

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

            var user = await _userRepository
                .GetUserByIdAsync(UserId);

            if (user == null)
                return RedirectToAction("ErrorIndex", "Home");

            if (string.IsNullOrWhiteSpace(model.PasswordSubmission)
                || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                || !string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                model.Error = _localizer["InvalidForm"];
            else
            {
                var success = await _userRepository
                    .ResetUserKnownPasswordAsync(
                        user.Login,
                        _crypter.Encrypt(model.PasswordSubmission),
                        _crypter.Encrypt(model.PasswordCreate1Submission));

                if (success)
                    model.Error = _localizer["ResetPasswordError"];
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
}
