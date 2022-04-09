using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
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
        private readonly IBadgeService _badgeService;
        private readonly IUserRepository _userRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IClock _clock;
        private readonly IStringLocalizer<Translations> _resources;

        public PlayerController(IPlayerRepository playerRepository,
            IClock clock,
            IBadgeService badgeService,
            IUserRepository userRepository,
            IPlayerService playerService,
            IStringLocalizer<Translations> resources)
        {
            _playerRepository = playerRepository;
            _badgeService = badgeService;
            _userRepository = userRepository;
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
            var dtos = await _playerRepository
                .GetPendingValidationPlayersAsync()
                .ConfigureAwait(false);

            var users = new Dictionary<ulong, UserDto>();
            foreach (var usrId in dtos.Select(dto => dto.CreationUserId).Distinct())
            {
                var user = await _userRepository
                    .GetUserByIdAsync(usrId)
                    .ConfigureAwait(false);
                users.Add(usrId, user);
            }

            var players = new List<Player>(dtos.Count);
            foreach (var p in dtos)
            {
                var pInfo = await _playerService
                    .GetPlayerInfoAsync(p)
                    .ConfigureAwait(false);

                players.Add(new Player(pInfo, users.Values));
            }

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

            var p = await _playerRepository
                .GetPlayerByIdAsync(request.PlayerId)
                .ConfigureAwait(false);

            if (p == null)
                return NotFound(_resources["PlayerDoesNotExist"]);

            if (p.ProposalDate.HasValue || p.RejectDate.HasValue)
                return Conflict(_resources["RejectAndProposalDateCombined"]);

            if (request.IsAccepted)
            {
                await _playerService
                    .AcceptSubmittedPlayerAsync(request, p.Clue, p.EasyClue)
                    .ConfigureAwait(false);

                var added = await _badgeService
                    .AddBadgeToUserAsync(Badges.DoItYourself, p.CreationUserId)
                    .ConfigureAwait(false);

                // if the badge for the first submission is added,
                // the badge for the 5th submission can't be added too
                if (!added)
                {
                    var players = await _playerRepository
                        .GetPlayersByCreatorAsync(p.CreationUserId, true)
                        .ConfigureAwait(false);

                    if (players.Count == 5)
                    {
                        await _badgeService
                            .AddBadgeToUserAsync(Badges.WeAreKikole, p.CreationUserId)
                            .ConfigureAwait(false);
                    }
                }

                // TODO: notify (+ badge)
            }
            else
            {
                await _playerRepository
                    .RefusePlayerProposalAsync(request.PlayerId)
                    .ConfigureAwait(false);

                // TODO: notify refusal
            }

            return NoContent();
        }

        [HttpGet("/users/known-players")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(IReadOnlyCollection<string>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<string>>> GetKnownPlayersAsync(
            [FromQuery] ulong userId)
        {
            var names = await _playerRepository
                .GetKnownPlayerNamesAsync(userId)
                .ConfigureAwait(false);

            return Ok(names);
        }
    }
}
