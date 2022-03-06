using System.Net;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Controllers
{
    [Route("clubs")]
    public class ClubController : KikoleBaseController
    {
        private readonly IClubRepository _clubRepository;

        public ClubController(IClubRepository clubRepository,
            IHttpContextAccessor httpContextAccessor,
            ICrypter crypter,
            IConfiguration configuration)
            : base(httpContextAccessor, crypter, configuration)
        {
            _clubRepository = clubRepository;
        }

        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateClubAsync([FromBody] ClubRequest request)
        {
            if (!IsAdminAuthentification())
                return Unauthorized();

            if (request == null)
                return BadRequest("Invalid request: null");

            var validityRequest = request.IsValid();
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest($"Invalid request: {validityRequest}");

            var playerId = await _clubRepository
                .CreateClubAsync(request.ToDto())
                .ConfigureAwait(false);

            if (playerId <= 0)
                return StatusCode((int)HttpStatusCode.InternalServerError, "Club creation failure");
            
            return Created($"clubs/{playerId}", null);
        }
    }
}
