using System;
using System.Globalization;
using System.Threading.Tasks;
using KikoleSite.Helpers;
using KikoleSite.Interfaces;
using KikoleSite.Interfaces.Repositories;
using KikoleSite.Interfaces.Services;
using KikoleSite.Models.Requests;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Controllers
{
    public class AccountController : KikoleBaseController
    {
        private readonly IStringLocalizer<AccountController> _localizer;

        public AccountController(IStringLocalizer<AccountController> localizer,
            IUserRepository userRepository,
            ICrypter crypter,
            IStringLocalizer<Translations> resources,
            IInternationalRepository internationalRepository,
            IClock clock,
            IPlayerService playerService,
            IClubRepository clubRepository,
            IBadgeService badgeService)
            : base(userRepository,
                crypter,
                resources,
                internationalRepository,
                clock,
                playerService,
                clubRepository,
                badgeService)
        {
            _localizer = localizer;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var (_, login) = GetAuthenticationCookie();

            return View(new AccountModel
            {
                IsAuthenticated = login != null,
                Login = login
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
                {
                    model.Error = _localizer["InvalidForm"];
                }
                else
                {
                    var (success, value) = await LoginAsync(
                            model.LoginSubmission, model.PasswordSubmission)
                        .ConfigureAwait(false);
                    if (success)
                    {
                        SetAuthenticationCookie(value, model.LoginSubmission);
                        return RedirectToAction("Index", "Home");
                    }
                    else
                        model.Error = value;
                }
            }
            else if (submitFrom == "getloginquestion")
            {
                if (string.IsNullOrWhiteSpace(model.LoginRecoverySubmission))
                {
                    model.Error = _localizer["InvalidForm"];
                }
                else
                {
                    // get question from login
                    var (ok, msg) = await GetLoginQuestionAsync(
                            model.LoginRecoverySubmission)
                        .ConfigureAwait(false);
                    if (ok)
                        model.QuestionRecovery = msg;
                    else
                        model.Error = msg;
                }
            }
            else if (submitFrom == "resetpassword")
            {
                if (string.IsNullOrWhiteSpace(model.LoginRecoverySubmission)
                    || string.IsNullOrWhiteSpace(model.RecoveryACreate)
                    || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                    || !string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                {
                    model.Error = _localizer["InvalidForm"];
                }
                else
                {
                    var response = await ResetPasswordAsync(
                            model.LoginRecoverySubmission, model.RecoveryACreate, model.PasswordCreate1Submission)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(response))
                        model.Error = response;
                    else
                        model.SuccessInfo = _localizer["PasswordReset"];
                }
            }
            else if (submitFrom == "resetqanda")
            {
                if (string.IsNullOrWhiteSpace(model.RecoveryQCreate)
                    || string.IsNullOrWhiteSpace(model.RecoveryACreate))
                {
                    model.Error = _localizer["InvalidForm"];
                }
                else
                {
                    var (token, login) = GetAuthenticationCookie();
                    var response = await ChangeQAndAAsync(
                            token, model.RecoveryQCreate, model.RecoveryACreate)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(response))
                        model.Error = response;
                    else
                    {
                        model.SuccessInfo = _localizer["QandAUpdated"];
                        model.IsAuthenticated = true;
                        model.Login = login;
                    }
                }
            }
            else if (submitFrom == "create")
            {
                if (string.IsNullOrWhiteSpace(model.LoginCreateSubmission)
                    || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                    || string.IsNullOrWhiteSpace(model.RegistrationId))
                {
                    model.Error = _localizer["InvalidForm"];
                }
                else if (!System.Guid.TryParse(model.RegistrationId, out var registrationId))
                {
                    model.Error = _localizer["InvalidRegistrationGuidFormat"];
                }
                else if (!string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                {
                    model.Error = _localizer["NotMatchingPassword"];
                }
                else if (model.PasswordCreate1Submission.Length < 6)
                {
                    model.Error = _localizer["TooShortPassword"];
                }
                else if (model.LoginCreateSubmission.Length < 3)
                {
                    model.Error = _localizer["TooShortLogin"];
                }
                else
                {
                    var value = await CreateAccountAsync(
                            model.LoginCreateSubmission,
                            model.PasswordCreate1Submission,
                            model.RecoveryQCreate,
                            model.RecoveryACreate,
                            Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                            registrationId)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        model.Error = value;
                    }
                    else
                    {
                        return await Index(new AccountModel
                        {
                            LoginSubmission = model.LoginCreateSubmission,
                            PasswordSubmission = model.PasswordCreate1Submission,
                            ForceLoginAction = true
                        }).ConfigureAwait(false);
                    }
                }
            }
            else if (submitFrom == "changepassword")
            {
                if (string.IsNullOrWhiteSpace(model.PasswordSubmission)
                    || string.IsNullOrWhiteSpace(model.PasswordCreate1Submission)
                    || !string.Equals(model.PasswordCreate1Submission, model.PasswordCreate2Submission))
                {
                    model.Error = _localizer["InvalidForm"];
                }
                else
                {
                    var (token, login) = GetAuthenticationCookie();
                    var response = await ChangePasswordAsync(
                            token, model.PasswordSubmission, model.PasswordCreate1Submission)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(response))
                        model.Error = response;
                    else
                    {
                        model.IsAuthenticated = true;
                        model.Login = login;
                        model.SuccessInfo = _localizer["PasswordChanged"];
                    }
                }
            }

            return View(model);
        }

        private async Task<(bool, string)> LoginAsync(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
                return (false, _resources["InvalidLogin"]);

            if (string.IsNullOrWhiteSpace(password))
                return (false, _resources["InvalidPassword"]);

            var existingUser = await _userRepository
                .GetUserByLoginAsync(login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser == null)
                return (false, _resources["UserDoesNotExist"]);

            if (!_crypter.Encrypt(password).Equals(existingUser.Password))
                return (false, _resources["PasswordDoesNotMatch"]);

            var value = $"{existingUser.Id}_{existingUser.UserTypeId}";

            var token = $"{value}_{_crypter.Encrypt(value)}";

            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "fr"
                    ? "Echec de l'authentification : jeton invalide"
                    : "Authentication failed: invalid token");
            }

            return (true, token);
        }

        private async Task<(bool, string)> GetLoginQuestionAsync(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return (false, _resources["InvalidLogin"]);

            var user = await _userRepository
                .GetUserByLoginAsync(login)
                .ConfigureAwait(false);

            if (user == null)
                return (false, _resources["UserDoesNotExist"]);

            return (true, user.PasswordResetQuestion);
        }

        private async Task<string> ResetPasswordAsync(string login, string answer, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(login))
                return _resources["InvalidLogin"];

            if (string.IsNullOrWhiteSpace(answer))
                return _resources["InvalidQOrA"];

            if (string.IsNullOrWhiteSpace(newPassword))
                return _resources["InvalidPassword"];

            var response = await _userRepository
                .ResetUserUnknownPasswordAsync(
                    login,
                    _crypter.Encrypt(answer),
                    _crypter.Encrypt(newPassword))
                .ConfigureAwait(false);

            if (!response)
                return _resources["ResetPasswordError"];

            return null;
        }

        private async Task<string> ChangeQAndAAsync(string authToken,
            string question, string answer)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return _resources["InvalidUser"];

            if (string.IsNullOrWhiteSpace(question)
                || string.IsNullOrWhiteSpace(answer))
                return _resources["InvalidQOrA"];

            await _userRepository
                .ResetUserQAndAAsync(userId, question, _crypter.Encrypt(answer))
                .ConfigureAwait(false);

            return null;
        }

        private async Task<string> CreateAccountAsync(string login, string password, string question, string answer, string ip, Guid registrationId)
        {
            var request = new UserRequest
            {
                Login = login,
                Password = password,
                PasswordResetQuestion = question,
                PasswordResetAnswer = answer?.Trim(),
                Ip = ip
            };

            if (request == null)
                return string.Format(_resources["InvalidRequest"], "null");

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return string.Format(_resources["InvalidRequest"], validityRequest);

            var existingUser = await _userRepository
                .GetUserByLoginAsync(request.Login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser != null)
                return _resources["AlreadyExistsAccount"];

            var registration = await _userRepository
                .GetRegistrationGuidAsync(registrationId.ToString())
                .ConfigureAwait(false);

            if (registration == null)
                return _resources["InvalidRegistrationId"];

            if (registration.UserId.HasValue)
                return _resources["UsedRegistrationId"];

            var userId = await _userRepository
                .CreateUserAsync(request.ToDto(_crypter))
                .ConfigureAwait(false);

            if (userId == 0)
                return _resources["UserCreationFailure"];

            await _userRepository
                .LinkRegistrationGuidToUserAsync(registrationId.ToString(), userId)
                .ConfigureAwait(false);

            return null;
        }

        private async Task<string> ChangePasswordAsync(string authToken,
            string currentPassword, string newPassword)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return _resources["InvalidUser"];

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return _resources["InvalidPassword"];

            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return _resources["UserDoesNotExist"];

            var success = await _userRepository
                .ResetUserKnownPasswordAsync(
                    user.Login,
                    _crypter.Encrypt(currentPassword),
                    _crypter.Encrypt(newPassword))
                .ConfigureAwait(false);

            if (!success)
                return _resources["ResetPasswordError"];

            return null;
        }

        private void SetAuthenticationCookie(string token, string login)
        {
            SetCookie(_cryptedAuthenticationCookieName,
                $"{token}{CookiePartsSeparator}{login}",
                DateTime.Now.AddMonths(1));
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
    }
}
