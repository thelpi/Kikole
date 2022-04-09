using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleApi.Controllers
{
    [Route("users")]
    public class UserController : KikoleBaseController
    {
        private readonly IUserRepository _userRepository;
        private readonly ICrypter _crypter;
        private readonly IStringLocalizer<Translations> _resources;

        public UserController(IUserRepository userRepository,
            ICrypter crypter,
            IStringLocalizer<Translations> resources)
        {
            _userRepository = userRepository;
            _crypter = crypter;
            _resources = resources;
        }

        [HttpPut("/user-guids")]
        [AuthenticationLevel(UserTypes.Administrator)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Guid>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Guid>>> GenerateUserGuidsAsync([FromQuery] ushort count)
        {
            var guids = new List<Guid>();

            for (var i = 0; i < count; i++)
            {
                var guid = Guid.NewGuid();
                await _userRepository
                    .GenerateUserGuidAsync(guid)
                    .ConfigureAwait(false);
                guids.Add(guid);
            }

            return Ok(guids);
        }

        [HttpGet]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<User>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<User>>> GetUsersAsync()
        {
            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            return Ok(users.Select(u => new User(u)).ToList());
        }

        [HttpPost]
        [AuthenticationLevel]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> CreateUserAsync([FromBody] UserRequest request)
        {
            if (request == null)
                return BadRequest(string.Format(_resources["InvalidRequest"], "null"));

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest(string.Format(_resources["InvalidRequest"], validityRequest));
            
            var existingUser = await _userRepository
                .GetUserByLoginAsync(request.Login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser != null)
                return Conflict(_resources["AlreadyExistsAccount"]);

            var userId = await _userRepository
                .CreateUserAsync(request.ToDto(_crypter))
                .ConfigureAwait(false);

            if (userId == 0)
                return StatusCode((int)HttpStatusCode.InternalServerError, _resources["UserCreationFailure"]);

            return Created($"users/{userId}", null);
        }

        [HttpGet("{login}/authentication-tokens")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<string>> GetAuthenticationTokenAsync(
            [FromRoute] string login,
            [FromQuery][Required] string password)
        {
            if (string.IsNullOrWhiteSpace(login))
                return BadRequest(_resources["InvalidLogin"]);

            if (string.IsNullOrWhiteSpace(password))
                return BadRequest(_resources["InvalidPassword"]);

            var existingUser = await _userRepository
                .GetUserByLoginAsync(login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser == null)
                return NotFound(_resources["UserDoesNotExist"]);

            if (!_crypter.Encrypt(password).Equals(existingUser.Password))
                return StatusCode((int)HttpStatusCode.Unauthorized, _resources["PasswordDoesNotMatch"]);

            var value = $"{existingUser.Id}_{existingUser.UserTypeId}";

            return $"{value}_{_crypter.Encrypt(value)}";
        }

        [HttpPut("/user-passwords")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ChangePasswordAsync([FromQuery] ulong userId,
            [FromQuery] string oldp, [FromQuery] string newp)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            if (string.IsNullOrWhiteSpace(oldp) || string.IsNullOrWhiteSpace(newp))
                return BadRequest(_resources["InvalidPassword"]);

            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return NotFound(_resources["UserDoesNotExist"]);

            var success = await _userRepository
                .ResetUserKnownPasswordAsync(
                    user.Login,
                    _crypter.Encrypt(oldp),
                    _crypter.Encrypt(newp))
                .ConfigureAwait(false);

            if (!success)
                return StatusCode((int)HttpStatusCode.InternalServerError, _resources["ResetPasswordError"]);

            return NoContent();
        }

        [HttpPatch("/user-questions")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateUserQAndA([FromQuery] ulong userId,
            [FromQuery] string question,
            [FromQuery] string answer)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            if (string.IsNullOrWhiteSpace(question)
                || string.IsNullOrWhiteSpace(answer))
                return BadRequest(_resources["InvalidQOrA"]);

            await _userRepository
                .ResetUserQAndAAsync(userId, question, _crypter.Encrypt(answer))
                .ConfigureAwait(false);

            return NoContent();
        }

        [HttpPatch("/reset-passwords")]
        [AuthenticationLevel]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ResetPasswordAsync([FromQuery] string login,
            [FromQuery] string answer, [FromQuery] string newPassword)
        {
            if (string.IsNullOrWhiteSpace(login))
                return BadRequest(_resources["InvalidLogin"]);

            if (string.IsNullOrWhiteSpace(answer))
                return BadRequest(_resources["InvalidQOrA"]);

            if (string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(_resources["InvalidPassword"]);

            var response = await _userRepository
                .ResetUserUnknownPasswordAsync(
                    login,
                    _crypter.Encrypt(answer),
                    _crypter.Encrypt(newPassword))
                .ConfigureAwait(false);

            if (!response)
                return StatusCode((int)HttpStatusCode.InternalServerError, _resources["ResetPasswordError"]);

            return NoContent();
        }

        [HttpGet("/user-types")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<UserTypes>> GetUserAsync([FromQuery] ulong userId)
        {
            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return NotFound(_resources["UserDoesNotExist"]);

            return Ok((UserTypes)user.UserTypeId);
        }

        [HttpGet("{login}/questions")]
        [AuthenticationLevel]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<string>> GetLoginQuestionAsync([FromRoute] string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return BadRequest(_resources["InvalidLogin"]);

            var user = await _userRepository
                .GetUserByLoginAsync(login)
                .ConfigureAwait(false);

            if (user == null)
                return NotFound(_resources["UserDoesNotExist"]);

            return Ok(user.PasswordResetQuestion);
        }
    }
}
