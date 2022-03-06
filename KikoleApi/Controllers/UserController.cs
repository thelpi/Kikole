using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Controllers
{
    [Route("users")]
    public class UserController : KikoleBaseController
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository,
            ICrypter crypter,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
            : base(httpContextAccessor, crypter, configuration)
        {
            _userRepository = userRepository;
        }

        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> CreateUserAsync([FromBody] UserRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request: null");

            var validityRequest = request.IsValid();
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest($"Invalid request: {validityRequest}");
            
            var existingUser = await _userRepository
                .GetUserByLoginAsync(request.Login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser != null)
                return Conflict("A account already exists with this login");

            var userId = await _userRepository
                .CreateUserAsync(request.ToDto(Crypter))
                .ConfigureAwait(false);

            if (userId == 0)
                return StatusCode((int)HttpStatusCode.InternalServerError, "User creation failure");

            return Created($"users/{userId}", null);
        }

        [HttpGet("{login}/authentication-tokens")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<string>> GetAuthenticationTokenAsync(
            [FromRoute] string login,
            [FromQuery][Required] string password)
        {
            if (string.IsNullOrWhiteSpace(login))
                return BadRequest("Invalid request: empty login");

            if (string.IsNullOrWhiteSpace(password))
                return BadRequest("Invalid request: empty password");

            var existingUser = await _userRepository
                .GetUserByLoginAsync(login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser == null)
                return NotFound();

            if (!Crypter.Encrypt(password).Equals(existingUser.Password))
                return Unauthorized();

            var encryptedCookiePart = Crypter.Encrypt($"{existingUser.Id}_{existingUser.IsAdmin}");

            return $"{existingUser.Id}_{existingUser.IsAdmin}_{encryptedCookiePart}";
        }

        // TODO: change password
        // TODO: reset password with q&a
    }
}
