using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    [Route("players")]
    public class PlayerController : KikoleBaseController
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IClock _clock;

        public PlayerController(IPlayerRepository playerRepository, IClock clock)
        {
            _playerRepository = playerRepository;
            _clock = clock;
        }

        [HttpPost]
        [AuthenticationLevel(AuthenticationLevel.AdminAuthenticated)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreatePlayerAsync([FromBody] PlayerRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request: null");

            var validityRequest = request.IsValid(_clock.Now);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest($"Invalid request: {validityRequest}");

            if (!request.ProposalDate.HasValue && request.SetLatestProposalDate)
            {
                var latestDate = await _playerRepository
                    .GetLatestProposalDateasync()
                    .ConfigureAwait(false);
                request.ProposalDate = latestDate.AddDays(1).Date;
            }

            var playerId = await _playerRepository
                .CreatePlayerAsync(request.ToDto())
                .ConfigureAwait(false);

            if (playerId == 0)
                return StatusCode((int)HttpStatusCode.InternalServerError, "Player creation failure");
            
            foreach (var club in request.ToPlayerClubDtos(playerId))
            {
                await _playerRepository
                    .CreatePlayerClubsAsync(club)
                    .ConfigureAwait(false);
            }

            return Created($"players/{playerId}", null);
        }
    }
}
