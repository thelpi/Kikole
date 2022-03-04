using System.Net;
using System.Threading.Tasks;
using KikoleApi.Abstractions;
using KikoleApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    [Route("clubs")]
    public class ClubController : ControllerBase
    {
        private readonly IClubRepository _clubRepository;

        public ClubController(IClubRepository clubRepository)
        {
            _clubRepository = clubRepository;
        }

        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreateClubAsync([FromBody] ClubRequest request)
        {
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
