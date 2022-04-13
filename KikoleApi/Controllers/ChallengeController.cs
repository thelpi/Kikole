using System;
using System.Collections.Generic;
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
    public class ChallengeController : KikoleBaseController
    {
        private readonly IChallengeService _challengeService;
        private readonly IPlayerService _playerService;
        private readonly IStringLocalizer<Translations> _resources;
        private readonly IClock _clock;

        public ChallengeController(IChallengeService challengeService,
            IPlayerService playerService,
            IStringLocalizer<Translations> resources,
            IClock clock)
        {
            _challengeService = challengeService;
            _playerService = playerService;
            _clock = clock;
            _resources = resources;
        }

        [HttpPost("challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> CreateChallengeAsync(
            [FromBody] ChallengeRequest request,
            [FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            if (request == null)
                return BadRequest(string.Format(_resources["InvalidRequest"], "null"));

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest(string.Format(_resources["InvalidRequest"], validityRequest));

            if (request.GuestUserId == userId)
                return Conflict(_resources["CantChallengeYourself"]);

            var response = await _challengeService
                .CreateChallengeAsync(request, userId)
                .ConfigureAwait(false);

            if (response == ChallengeResponseError.ChallengeHostIsInvalid)
                return BadRequest(_resources["ChallengeHostIsInvalid"]);

            if (response == ChallengeResponseError.ChallengeCreatorIsAdmin)
                return Conflict(_resources["ChallengeCreatorIsAdmin"]);

            if (response == ChallengeResponseError.ChallengeOpponentIsInvalid)
                return Conflict(_resources["ChallengeOpponentIsInvalid"]);

            if (response == ChallengeResponseError.ChallengeOpponentIsAdmin)
                return Conflict(_resources["ChallengeOpponentIsAdmin"]);

            if (response == ChallengeResponseError.ChallengeAlreadyExist)
                return Conflict(_resources["ChallengeAlreadyExist"]);

            return StatusCode((int)HttpStatusCode.Created);
        }

        [HttpPatch("challenges/{id}")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> RespondToChallengeAsync(
            [FromRoute] ulong id,
            [FromQuery] ulong userId,
            [FromQuery] bool isAccepted)
        {
            if (id == 0)
                return BadRequest(_resources["InvalidChallengeId"]);

            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            var response = await _challengeService
                .RespondToChallengeAsync(id, userId, isAccepted)
                .ConfigureAwait(false);

            if (response == ChallengeResponseError.ChallengeNotFound)
                return NotFound(_resources["ChallengeNotFound"]);

            if (response == ChallengeResponseError.CantAutoAcceptChallenge)
                return Forbid(_resources["CantAutoAcceptChallenge"]);

            if (response == ChallengeResponseError.BothAcceptedAndCancelledChallenge)
                return Forbid(_resources["BothAcceptedAndCancelledChallenge"]);

            if (response == ChallengeResponseError.ChallengeAlreadyAccepted)
                return Conflict(_resources["ChallengeAlreadyAccepted"]);

            if (response == ChallengeResponseError.ChallengeAlreadyAnswered)
                return Conflict(_resources["ChallengeAlreadyAnswered"]);

            if (response == ChallengeResponseError.InvalidOpponentAccount)
                return Conflict(_resources["InvalidOpponentAccount"]);

            return NoContent();
        }

        [HttpGet("waiting-challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Challenge>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Challenge>>> GetChallengesWaitingForResponseAsync(
            [FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            var challenges = await _challengeService
                .GetPendingChallengesAsync(userId)
                .ConfigureAwait(false);

            return Ok(challenges);
        }

        [HttpGet("requested-challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Challenge>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Challenge>>> GetRequestedChallengesAsync(
            [FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            var challenges = await _challengeService
                .GetRequestedChallengesAsync(userId)
                .ConfigureAwait(false);

            return Ok(challenges);
        }

        [HttpGet("accepted-challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Challenge>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Challenge>>> GetAcceptedChallengeAsync(
            [FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            var challenges = await _challengeService
                .GetAcceptedChallengesAsync(userId)
                .ConfigureAwait(false);

            return Ok(challenges);
        }

        [HttpGet("history-challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(Challenge), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Challenge>>> GetChallengesHistoryAsync(
            [FromQuery] ulong userId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            var debut = await _playerService
                .GetFirstSubmittedPlayerDateAsync(false)
                .ConfigureAwait(false);

            if (fromDate.HasValue && fromDate.Value.Date < debut.Date)
                return BadRequest(_resources["InvalidDateRange"]);

            var yesterday = _clock.Now.AddDays(-1).Date;
            if (toDate.HasValue && toDate.Value.Date < yesterday)
                return BadRequest(_resources["InvalidDateRange"]);

            if (toDate.HasValue && fromDate.HasValue && toDate.Value.Date < fromDate.Value.Date)
                return BadRequest(_resources["InvalidDateRange"]);

            var challenges = await _challengeService
                .GetChallengesHistoryAsync(userId, fromDate?.Date ?? debut.Date, toDate?.Date ?? yesterday)
                .ConfigureAwait(false);

            return Ok(challenges);
        }
    }
}
