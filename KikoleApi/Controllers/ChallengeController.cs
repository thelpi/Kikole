using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class ChallengeController : KikoleBaseController
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IPlayerService _playerService;
        private readonly IBadgeService _badgeService;
        private readonly TextResources _resources;
        private readonly IClock _clock;

        public ChallengeController(IChallengeRepository challengeRepository,
            IUserRepository userRepository,
            ILeaderRepository leaderRepository,
            IBadgeService badgeService,
            IPlayerService playerService,
            TextResources resources,
            IClock clock)
        {
            _challengeRepository = challengeRepository;
            _playerService = playerService;
            _userRepository = userRepository;
            _leaderRepository = leaderRepository;
            _badgeService = badgeService;
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
            [FromBody] ChallengeRequest request, [FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources.InvalidUser);

            if (request == null)
                return BadRequest(string.Format(_resources.InvalidRequest, "null"));

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest(string.Format(_resources.InvalidRequest, validityRequest));

            if (request.GuestUserId == userId)
                return Conflict(_resources.CantChallengeYourself);

            var hostUser = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (hostUser == null)
                return BadRequest(_resources.ChallengeHostIsInvalid);

            if (hostUser.UserTypeId == (ulong)UserTypes.Administrator)
                return Conflict(_resources.ChallengeCreatorIsAdmin);

            var guestUser = await _userRepository
                .GetUserByIdAsync(request.GuestUserId)
                .ConfigureAwait(false);

            if (hostUser == null)
                return Conflict(_resources.ChallengeOpponentIsInvalid);

            if (hostUser.UserTypeId == (ulong)UserTypes.Administrator)
                return Conflict(_resources.ChallengeOpponentIsAdmin);

            var challengeAlready = await _challengeRepository
                .GetUsersFutureChallengesAsync(userId, request.GuestUserId)
                .ConfigureAwait(false);

            if (challengeAlready.Count > 0)
                return Conflict(_resources.ChallengeAlreadyExist);

            var challengeId = await _challengeRepository
                .CreateChallengeAsync(request.ToDto(userId))
                .ConfigureAwait(false);

            return Created($"challenges/{challengeId}", null);
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
                return BadRequest(_resources.InvalidChallengeId);

            if (userId == 0)
                return BadRequest(_resources.InvalidUser);

            var challenge = await _challengeRepository
                .GetChallengeByIdAsync(id)
                .ConfigureAwait(false);

            if (challenge == null)
                return NotFound(_resources.ChallengeNotFound);

            var isCancel = challenge.HostUserId == userId;

            if (!isCancel && challenge.GuestUserId != userId)
                return Forbid(_resources.CantAutoAcceptChallenge);

            if (isCancel && isAccepted)
                return Forbid(_resources.BothAcceptedAndCancelledChallenge);

            if (isCancel && challenge.IsAccepted.HasValue)
                return Conflict(_resources.ChallengeAlreadyAccepted);

            if (!isCancel && challenge.IsAccepted.HasValue)
                return Conflict(_resources.ChallengeAlreadyAnswered);

            if (!isAccepted)
            {
                // date is irrelevant in this case
                await _challengeRepository
                   .RespondToChallengeAsync(id, isAccepted, _clock.Now)
                   .ConfigureAwait(false);

                return NoContent();
            }

            var hostUser = await _userRepository
                .GetUserByIdAsync(challenge.HostUserId)
                .ConfigureAwait(false);

            if (hostUser == null)
            {
                // date is irrelevant in this case
                await _challengeRepository
                   .RespondToChallengeAsync(id, isAccepted, _clock.Now)
                   .ConfigureAwait(false);

                return Conflict(_resources.InvalidOpponentAccount);
            }

            var hostDates = await _challengeRepository
                .GetBookedChallengesAsync(challenge.HostUserId)
                .ConfigureAwait(false);

            var guestDates = await _challengeRepository
                .GetBookedChallengesAsync(challenge.GuestUserId)
                .ConfigureAwait(false);

            var challengeDate = await _playerService
                .ComputeAvailableChallengeDateAsync(challenge, hostDates, guestDates)
                .ConfigureAwait(false);

            await _challengeRepository
                .RespondToChallengeAsync(id, isAccepted, challengeDate)
                .ConfigureAwait(false);

            if (isAccepted)
            {
                foreach (var user in new[] { challenge.HostUserId, challenge.GuestUserId })
                {
                    await _badgeService
                        .AddBadgeToUserAsync(Badges.ChallengeAccepted, user)
                        .ConfigureAwait(false);

                    if (challenge.PointsRate >= 80)
                    {
                        await _badgeService
                            .AddBadgeToUserAsync(Badges.AllIn, user)
                            .ConfigureAwait(false);
                    }
                }

                var allAccepted = await _challengeRepository
                    .GetAcceptedChallengesAsync(null, null)
                    .ConfigureAwait(false);

                if (allAccepted.Count(ac => ac.HostUserId == challenge.HostUserId) >= 5)
                {
                    await _badgeService
                        .AddBadgeToUserAsync(Badges.GambleAddiction, challenge.HostUserId)
                        .ConfigureAwait(false);
                }
            }

            return NoContent();
        }

        [HttpGet("waiting-challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Challenge>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Challenge>>> GetChallengesWaitingForResponseAsync([FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources.InvalidUser);

            var dtos = await _challengeRepository
                .GetPendingChallengesByGuestUserAsync(userId)
                .ConfigureAwait(false);

            var challenges = new List<Challenge>();
            foreach (var challenge in dtos)
            {
                var hostUser = await _userRepository
                    .GetUserByIdAsync(challenge.HostUserId)
                    .ConfigureAwait(false);

                challenges.Add(new Challenge(challenge, hostUser.Login, userId));
            }

            return Ok(challenges);
        }

        [HttpGet("requested-challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Challenge>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Challenge>>> GetRequestedChallengesAsync([FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources.InvalidUser);

            var dtos = await _challengeRepository
                .GetPendingChallengesByHostUserAsync(userId)
                .ConfigureAwait(false);

            var challenges = new List<Challenge>();
            foreach (var c in dtos)
            {
                var guestUser = await _userRepository
                    .GetUserByIdAsync(c.GuestUserId)
                    .ConfigureAwait(false);

                challenges.Add(new Challenge(c, guestUser.Login, userId));
            }

            return Ok(challenges);
        }

        [HttpGet("accepted-challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Challenge>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Challenge>>> GetAcceptedChallengeAsync([FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources.InvalidUser);

            var dtos = await _challengeRepository
                .GetAcceptedChallengesAsync(_clock.Now, null)
                .ConfigureAwait(false);

            var challenges = new List<Challenge>();
            foreach (var challenge in dtos.Where(c => c.GuestUserId == userId || c.HostUserId == userId))
            {
                var opponentUser = await _userRepository
                    .GetUserByIdAsync(userId == challenge.HostUserId
                        ? challenge.GuestUserId
                        : challenge.HostUserId)
                    .ConfigureAwait(false);

                challenges.Add(new Challenge(challenge, opponentUser.Login, userId));
            }

            challenges = challenges
                .OrderBy(c => c.ChallengeDate)
                .ToList();

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
                return BadRequest(_resources.InvalidUser);

            var debut = await _playerService
                .GetFirstSubmittedPlayerDateAsync()
                .ConfigureAwait(false);

            if (fromDate.HasValue && fromDate.Value.Date < debut.Date)
                return BadRequest(_resources.InvalidDateRange);

            var yesterday = _clock.Now.AddDays(-1).Date;
            if (toDate.HasValue && toDate.Value.Date < yesterday)
                return BadRequest(_resources.InvalidDateRange);

            if (toDate.HasValue && fromDate.HasValue && toDate.Value.Date < fromDate.Value.Date)
                return BadRequest(_resources.InvalidDateRange);

            var dateBeginOk = fromDate?.Date ?? debut.Date;
            var dateEndOk = toDate?.Date ?? yesterday;

            var hostChallenges = await _challengeRepository
                .GetRequestedAcceptedChallengesAsync(userId, dateBeginOk, dateEndOk)
                .ConfigureAwait(false);

            var guestChallenges = await _challengeRepository
                .GetResponseAcceptedChallengesAsync(userId, dateBeginOk, dateEndOk)
                .ConfigureAwait(false);

            var challenges = new List<Challenge>(hostChallenges.Count + guestChallenges.Count);
            var usersCache = new Dictionary<ulong, string>();

            await AddChallengesAsync(
                    challenges, usersCache, hostChallenges, c => c.GuestUserId, userId)
                .ConfigureAwait(false);

            await AddChallengesAsync(
                    challenges, usersCache, guestChallenges, c => c.HostUserId, userId)
                .ConfigureAwait(false);

            return Ok(challenges.OrderByDescending(c => c.ChallengeDate).ToList());
        }

        private async Task AddChallengesAsync(List<Challenge> challenges,
            Dictionary<ulong, string> usersCache,
            IEnumerable<Models.Dtos.ChallengeDto> dtos,
            Func<Models.Dtos.ChallengeDto, ulong> getOpponentUserIdFunc,
            ulong userId)
        {
            foreach (var c in dtos)
            {
                // ChallengeDate.Value is safe here
                var leaders = await _leaderRepository
                    .GetLeadersAtDateAsync(c.ChallengeDate.Value)
                    .ConfigureAwait(false);

                var opponentUserId = getOpponentUserIdFunc(c);
                if (!usersCache.ContainsKey(opponentUserId))
                {
                    var user = await _userRepository
                        .GetUserByIdAsync(opponentUserId)
                        .ConfigureAwait(false);
                    usersCache.Add(opponentUserId, user.Login);
                }

                challenges.Add(new Challenge(c, usersCache[opponentUserId], userId, leaders));
            }
        }
    }
}
