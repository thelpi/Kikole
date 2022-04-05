using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    [Route("players")]
    public class PlayerController : KikoleBaseController
    {
        private readonly IBadgeService _badgeService;
        private readonly IUserRepository _userRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IClubRepository _clubRepository;
        private readonly IClock _clock;
        private readonly TextResources _resources;

        public PlayerController(IPlayerRepository playerRepository,
            IClock clock,
            IBadgeService badgeService,
            IUserRepository userRepository,
            IClubRepository clubRepository,
            TextResources resources)
        {
            _playerRepository = playerRepository;
            _badgeService = badgeService;
            _userRepository = userRepository;
            _clubRepository = clubRepository;
            _resources = resources;
            _clock = clock;
        }

        [HttpGet("/player-clues")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<string>> GetPlayerOfTheDayClueAsync([FromQuery][Required] DateTime proposalDate)
        {
            var player = await _playerRepository
                .GetPlayerOfTheDayAsync(proposalDate)
                .ConfigureAwait(false);

            var clue = player.Clue;
            if (_resources.Language != Languages.en)
            {
                clue = await _playerRepository
                    .GetClueAsync(player.Id, (ulong)_resources.Language)
                    .ConfigureAwait(false);
            }

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
                return BadRequest(_resources.InvalidUser);

            if (request == null)
                return BadRequest(string.Format(_resources.InvalidRequest, "null"));

            var validityRequest = request.IsValid(_clock.Now, _resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest(string.Format(_resources.InvalidRequest, validityRequest));

            if (!request.ProposalDate.HasValue && request.SetLatestProposalDate)
                request.ProposalDate = await GetNextDateAsync().ConfigureAwait(false);

            var playerId = await _playerRepository
                .CreatePlayerAsync(request.ToDto(userId))
                .ConfigureAwait(false);

            if (playerId == 0)
                return StatusCode((int)HttpStatusCode.InternalServerError, _resources.PlayerCreationFailure);

            var languagesClues = new Dictionary<ulong, string>();
            if (request.ClueLanguages != null)
            {
                foreach (var kvp in request.ClueLanguages)
                {
                    var actualValue = kvp.Value?.Trim();
                    if(!string.IsNullOrWhiteSpace(actualValue))
                        languagesClues.Add((ulong)kvp.Key, actualValue);
                }
            }

            if (languagesClues.Count > 0)
            {
                await _playerRepository
                    .InsertPlayerCluesByLanguageAsync(playerId, languagesClues)
                    .ConfigureAwait(false);
            }

            foreach (var club in request.ToPlayerClubDtos(playerId))
            {
                await _playerRepository
                    .CreatePlayerClubsAsync(club)
                    .ConfigureAwait(false);
            }

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

            var usersCache = new Dictionary<ulong, UserDto>();

            var players = new List<Player>(dtos.Count);
            foreach (var p in dtos)
            {
                if (!usersCache.ContainsKey(p.CreationUserId))
                {
                    var user = await _userRepository
                        .GetUserByIdAsync(p.CreationUserId)
                        .ConfigureAwait(false);
                    usersCache.Add(p.CreationUserId, user);
                }

                var playerClubs = await _playerRepository
                    .GetPlayerClubsAsync(p.Id)
                    .ConfigureAwait(false);

                var playerClubsDetails = new List<ClubDto>(playerClubs.Count);
                foreach (var pc in playerClubs)
                {
                    var c = await _clubRepository
                        .GetClubAsync(pc.ClubId)
                        .ConfigureAwait(false);
                    playerClubsDetails.Add(c);
                }

                players.Add(new Player(p, usersCache.Values, playerClubs, playerClubsDetails));
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
                return BadRequest(string.Format(_resources.InvalidRequest, "null"));

            var validityCheck = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityCheck))
                return BadRequest(string.Format(_resources.InvalidRequest, validityCheck));

            var p = await _playerRepository
                .GetPlayerByIdAsync(request.PlayerId)
                .ConfigureAwait(false);

            if (p == null)
                return NotFound(_resources.PlayerDoesNotExist);

            if (p.ProposalDate.HasValue || p.RejectDate.HasValue)
                return Conflict(_resources.RejectAndProposalDateCombined);

            if (request.IsAccepted)
            {
                var clueEn = string.IsNullOrWhiteSpace(request.ClueEditEn)
                    ? p.Clue
                    : request.ClueEditEn.Trim();

                var latestDate = await GetNextDateAsync().ConfigureAwait(false);

                await _playerRepository
                    .ValidatePlayerProposalAsync(request.PlayerId, clueEn, latestDate)
                    .ConfigureAwait(false);

                var languagesClues = new Dictionary<ulong, string>();
                foreach (var kvp in request.ClueEditLangugages)
                    languagesClues.Add((ulong)kvp.Key, kvp.Value.Trim());

                await _playerRepository
                    .InsertPlayerCluesByLanguageAsync(request.PlayerId, languagesClues)
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

        private async Task<DateTime> GetNextDateAsync()
        {
            var latestDate = await _playerRepository
                .GetLatestProposalDateAsync()
                .ConfigureAwait(false);
            return latestDate.AddDays(1).Date;
        }
    }
}
