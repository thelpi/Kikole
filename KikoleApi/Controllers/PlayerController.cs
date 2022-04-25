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
    /// <summary>
    /// Player controller.
    /// </summary>
    /// <seealso cref="KikoleBaseController"/>
    [Route("players")]
    public class PlayerController : KikoleBaseController
    {
        private readonly IPlayerService _playerService;
        private readonly IBadgeService _badgeService;
        private readonly IClock _clock;
        private readonly IStringLocalizer<Translations> _resources;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="clock">Clock service.</param>
        /// <param name="playerService">Instance of <see cref="IPlayerService"/>.</param>
        /// <param name="badgeService">Instance of <see cref="IBadgeService"/>.</param>
        /// <param name="resources">Translation resources.</param>
        public PlayerController(IClock clock,
            IPlayerService playerService,
            IBadgeService badgeService,
            IStringLocalizer<Translations> resources)
        {
            _playerService = playerService;
            _badgeService = badgeService;
            _resources = resources;
            _clock = clock;
        }

        /// <summary>
        /// Gets the clue of a player at a specified date.
        /// </summary>
        /// <param name="proposalDate">Proposal date.</param>
        /// <param name="isEasy"><c>True</c> for easy clue.</param>
        /// <returns>The clue.</returns>
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

        /// <summary>
        /// Creates a player.
        /// </summary>
        /// <param name="request">Player creation request.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Created content.</returns>
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

        /// <summary>
        /// Gets every submission of players to manage by the administrator. 
        /// </summary>
        /// <returns>Collection of players.</returns>
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

        /// <summary>
        /// Validates a player submission.
        /// </summary>
        /// <param name="request">Validation request.</param>
        /// <returns>Created content.</returns>
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

            var (result, userId, badges) = await _playerService
                .ValidatePlayerSubmissionAsync(request)
                .ConfigureAwait(false);

            if (result == PlayerSubmissionErrors.PlayerNotFound)
                return NotFound(_resources["PlayerDoesNotExist"]);

            if (result == PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused)
                return Conflict(_resources["RejectAndProposalDateCombined"]);

            foreach (var badge in badges)
            {
                await _badgeService
                    .AddBadgeToUserAsync(badge, userId)
                    .ConfigureAwait(false);
            }

            return NoContent();
        }

        /// <summary>
        /// Get names of every player known by a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of names.</returns>
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

        /// <summary>
        /// Gets the view on a player's name from the point of view of a specified user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="proposalDate">Proposal date.</param>
        /// <returns>Instance of <see cref="PlayerCreator"/>.</returns>
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

        /// <summary>
        /// Reassign player' dates starting tomorrow.
        /// </summary>
        /// <returns>No content.</returns>
        [HttpPut("/players-dates")]
        [AuthenticationLevel(UserTypes.Administrator)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ReassignPlayersOfTheDayAsync()
        {
            await _playerService
                .ReassignPlayersOfTheDayAsync()
                .ConfigureAwait(false);

            return NoContent();
        }

        /// <summary>
        /// Gets statistics about players until today.
        /// </summary>
        /// <param name="userId">Identifier of user who does the request.</param>
        /// <param name="sortTypes">Collection of sort options.</param>
        /// <param name="sortDesc">For each <paramref name="sortTypes"/>, indicates if descending sort.</param>
        /// <returns>Collection of statistics; some info might be anonymised.</returns>
        [HttpGet("/player-statistics")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(IReadOnlyCollection<PlayerStat>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<PlayerStat>>> GetPlayersStatisticsAsync(
            [FromQuery] ulong userId,
            [FromQuery] string[] sortTypes,
            [FromQuery] bool[] sortDesc)
        {
            var parsedSorts = new List<(PlayerStatSorts, bool)>(sortTypes?.Length ?? 0);
            if (sortTypes != null)
            {
                for (var i = 0; i < sortTypes.Length; i++)
                {
                    if (Enum.TryParse<PlayerStatSorts>(sortTypes[i], out var sortType))
                        parsedSorts.Add((sortType, sortDesc?.Length > i && sortDesc[i]));
                }
            }

            var stats = await _playerService
                .GetPlayersStatisticsAsync(userId, parsedSorts?.ToArray())
                .ConfigureAwait(false);

            return Ok(stats);
        }
    }
}
