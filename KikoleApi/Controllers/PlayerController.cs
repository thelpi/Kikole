using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Interfaces.Services;
using KikoleApi.Models;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleApi.Controllers
{
    [Route("players")]
    public class PlayerController : KikoleBaseController
    {
        private readonly IPlayerService _playerService;
        private readonly IClock _clock;
        private readonly IStringLocalizer<Translations> _resources;

        public PlayerController(IClock clock,
            IPlayerService playerService,
            IStringLocalizer<Translations> resources)
        {
            _playerService = playerService;
            _resources = resources;
            _clock = clock;
        }

        [HttpGet("/player-clues")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<string>> GetPlayerOfTheDayClueAsync(
            [FromQuery][Required] DateTime proposalDate,
            [FromQuery] bool isEasy)
        {
            var clue = await _playerService
                .GetPlayerClueAsync(proposalDate, isEasy)
                .ConfigureAwait(false);

            return Ok(clue);
        }

        [HttpPost]
        [AuthenticationLevel(UserTypes.PowerUser)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreatePlayerAsync(
            [FromBody] PlayerRequest request,
            [FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            if (request == null)
                return BadRequest(string.Format(_resources["InvalidRequest"], "null"));

            var validityRequest = request.IsValid(_clock.Today, _resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest(string.Format(_resources["InvalidRequest"], validityRequest));

            var playerId = await _playerService
                .CreatePlayerAsync(request, userId)
                .ConfigureAwait(false);

            return Created($"players/{playerId}", null);
        }


        [HttpGet("/player-submissions")]
        [AuthenticationLevel(UserTypes.Administrator)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Player>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Player>>> GetPlayerSubmissionsAsync()
        {
            var players = await _playerService
                .GetPlayerSubmissionsAsync()
                .ConfigureAwait(false);

            return Ok(players);
        }

        [HttpPost("/player-submissions")]
        [AuthenticationLevel(UserTypes.Administrator)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> ValidatePlayerSubmissionAsync(
            [FromBody] PlayerSubmissionValidationRequest request)
        {
            if (request == null)
                return BadRequest(string.Format(_resources["InvalidRequest"], "null"));

            var validityCheck = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityCheck))
                return BadRequest(string.Format(_resources["InvalidRequest"], validityCheck));

            var result = await _playerService
                .ValidatePlayerSubmissionAsync(request)
                .ConfigureAwait(false);

            if (result == PlayerSubmissionErrors.PlayerNotFound)
                return NotFound(_resources["PlayerDoesNotExist"]);

            if (result == PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused)
                return Conflict(_resources["RejectAndProposalDateCombined"]);

            return NoContent();
        }

        [HttpGet("/users/known-players")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(IReadOnlyCollection<string>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<string>>> GetKnownPlayersAsync(
            [FromQuery] ulong userId)
        {
            var names = await _playerService
                .GetKnownPlayerNamesAsync(userId)
                .ConfigureAwait(false);

            return Ok(names);
        }

        [HttpGet("/player-of-the-day-users")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(PlayerCreator), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<PlayerCreator>> GetPlayerOfTheDayFromUserPovAsync(
            [FromQuery] ulong userId, [FromQuery] DateTime proposalDate)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            var p = await _playerService
                .GetPlayerOfTheDayFromUserPovAsync(userId, proposalDate)
                .ConfigureAwait(false);

            return base.Ok(p);
        }
    }
}
