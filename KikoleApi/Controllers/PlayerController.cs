using System.Net;
using System.Threading.Tasks;
using KikoleApi.Domain.Models;
using KikoleApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    [Route("players")]
    public class PlayerController : ControllerBase
    {
        private readonly IPlayerRepository _playerRepository;

        public PlayerController(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreatePlayerAsync([FromBody] PlayerRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request: null");

            var validityRequest = request.IsValid();
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest($"Invalid request: {validityRequest}");

            var playerId = await _playerRepository
                .CreatePlayerAsync(request.ToDto())
                .ConfigureAwait(false);

            if (playerId <= 0)
                return StatusCode((int)HttpStatusCode.InternalServerError, "Player creation failure");

            foreach (var name in request.AllowedNames.ToDtos(playerId))
            {
                await _playerRepository
                    .CreatePlayerNamesAsync(name)
                    .ConfigureAwait(false);
            }
            
            foreach (var club in request.Clubs.ToDtos(playerId))
            {
                await _playerRepository
                    .CreatePlayerClubsAsync(club)
                    .ConfigureAwait(false);
            }

            return Created($"players/{playerId}", null);
        }
    }
}
