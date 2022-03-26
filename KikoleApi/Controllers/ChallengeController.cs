using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class ChallengeController : KikoleBaseController
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IClock _clock;
        private readonly IPlayerRepository _playerRepository;
        private readonly IUserRepository _userRepository;

        public ChallengeController(IChallengeRepository challengeRepository,
            IPlayerRepository playerRepository,
            IUserRepository userRepository,
            IClock clock)
        {
            _challengeRepository = challengeRepository;
            _playerRepository = playerRepository;
            _userRepository = userRepository;
            _clock = clock;
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
            if (userId == 0 || request?.IsValid() != true)
                return BadRequest();

            if (request.GuestUserId == userId)
                return Conflict("You can't challenge yourself");

            if (_clock.Now.Hour == 23)
                return Conflict("Can't create challenge; it's past 11PM.");

            var hostUser = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (hostUser == null)
                return BadRequest();

            if (hostUser.UserTypeId == (ulong)UserTypes.Administrator)
                return Conflict("Can't create challenge; you're administrator");

            var guestUser = await _userRepository
                .GetUserByIdAsync(request.GuestUserId)
                .ConfigureAwait(false);

            if (hostUser == null)
                return Conflict("Can't create challenge; invalid opponent account");

            if (hostUser.UserTypeId == (ulong)UserTypes.Administrator)
                return Conflict("Can't create challenge; opponent account is administrator");

            var challengeDate = _clock.Now.AddDays(1).Date;

            var playerOfTomorrow = await _playerRepository
                .GetPlayerOfTheDayAsync(challengeDate)
                .ConfigureAwait(false);

            if (playerOfTomorrow.CreationUserId == userId)
                return Conflict("Can't create challenge; next player is yours");

            if (playerOfTomorrow.CreationUserId == request.GuestUserId)
                return Conflict("Can't create challenge; next player is his");

            var hostChallenges = await _challengeRepository
                .GetChallengesByUserAndByDateAsync(userId, challengeDate)
                .ConfigureAwait(false);

            if (hostChallenges.Any(c => c.IsAccepted > 0))
                return Conflict("Can't create challenge; you're already in a challenge");

            if (hostChallenges.Any(c => c.GuestUserId == request.GuestUserId))
                return Conflict("Can't create challenge; already a request to this opponent");

            var guestChallenges = await _challengeRepository
                .GetChallengesByUserAndByDateAsync(request.GuestUserId, challengeDate)
                .ConfigureAwait(false);

            if (guestChallenges.Any(c => c.IsAccepted > 0))
                return Conflict("Can't create challenge; opponent is already in a challenge");

            var challengeId = await _challengeRepository
                .CreateChallengeAsync(request.ToDto(userId, challengeDate))
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
            if (id == 0 || userId == 0)
                return BadRequest();

            var challenge = await _challengeRepository
                .GetChallengeByIdAsync(id)
                .ConfigureAwait(false);

            if (challenge == null)
                return NotFound();

            var isCancel = challenge.HostUserId == userId;

            if (!isCancel && challenge.GuestUserId != userId)
                return Forbid();

            if (challenge.IsAccepted.HasValue)
            {
                if (isCancel)
                    return Conflict("You can't cancel an accepted challenge");
                else
                    return Conflict("You've already respond to this challenge");
            }

            if (isAccepted)
            {
                if (isCancel)
                    return Forbid();

                var hostUser = await _userRepository
                    .GetUserByIdAsync(challenge.HostUserId)
                    .ConfigureAwait(false);

                var conflictReasons = new List<string>();

                if (hostUser == null)
                    conflictReasons.Add("Opponent is an invalid account");

                var challengeDate = _clock.Now.AddDays(1).Date;

                if (challenge.ChallengeDate != challengeDate)
                    conflictReasons.Add("Challenge is obsolete");

                var guestChallenges = await _challengeRepository
                    .GetChallengesByUserAndByDateAsync(challenge.GuestUserId, challengeDate)
                    .ConfigureAwait(false);

                var hostChallenges = await _challengeRepository
                    .GetChallengesByUserAndByDateAsync(challenge.HostUserId, challengeDate)
                    .ConfigureAwait(false);

                // this is a security: it should not happen as
                // other challenges are refused when one is accepted
                if (guestChallenges.Any(c => c.IsAccepted > 0))
                    conflictReasons.Add("You've already accepted a challenge");

                // this is a security: it should not happen as
                // other challenges are refused when one is accepted
                if (hostChallenges.Any(c => c.IsAccepted > 0))
                    conflictReasons.Add("Opponent already accepted a challenge");

                if (conflictReasons.Count > 0)
                {
                    // cancels the challenge
                    await _challengeRepository
                        .RespondToChallengeAsync(id, false)
                        .ConfigureAwait(false);

                    return Conflict(string.Join("; ", conflictReasons));
                }

                foreach (var c in guestChallenges.Where(gc => !gc.IsAccepted.HasValue && gc.Id != id))
                {
                    await _challengeRepository
                        .RespondToChallengeAsync(c.Id, false)
                        .ConfigureAwait(false);
                }

                foreach (var c in hostChallenges.Where(hc => !hc.IsAccepted.HasValue && hc.Id != id))
                {
                    await _challengeRepository
                        .RespondToChallengeAsync(c.Id, false)
                        .ConfigureAwait(false);
                }
            }

            await _challengeRepository
                .RespondToChallengeAsync(id, isAccepted)
                .ConfigureAwait(false);

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
                return BadRequest();

            var challenges = await _challengeRepository
                .GetPendingChallengesByGuestUserAsync(userId)
                .ConfigureAwait(false);

            var challengeDate = _clock.Now.AddDays(1).Date;

            var okChallenges = new List<Challenge>();
            var koChallenges = new List<ulong>();
            foreach (var challenge in challenges)
            {
                if (challenge.ChallengeDate != challengeDate)
                    koChallenges.Add(challenge.Id);
                else
                {
                    var hostUser = await _userRepository
                        .GetUserByIdAsync(challenge.HostUserId)
                        .ConfigureAwait(false);

                    if (hostUser == null)
                        koChallenges.Add(challenge.Id);
                    else
                        okChallenges.Add(new Challenge(challenge, hostUser.Login, userId));
                }
            }

            foreach (var id in koChallenges)
            {
                await _challengeRepository
                    .RespondToChallengeAsync(id, false)
                    .ConfigureAwait(false);
            }

            return Ok(okChallenges);
        }

        [HttpGet("requested-challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Challenge>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Challenge>>> GetRequestedChallengesAsync([FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest();

            var challenges = await _challengeRepository
                .GetChallengesByUserAndByDateAsync(userId, _clock.Now.AddDays(1).Date)
                .ConfigureAwait(false);

            var okChallenges = new List<Challenge>();
            foreach (var c in challenges.Where(c => c.HostUserId == userId && c.IsAccepted == null))
            {
                var guestUser = await _userRepository
                    .GetUserByIdAsync(c.GuestUserId)
                    .ConfigureAwait(false);

                if (guestUser == null)
                {
                    await _challengeRepository
                        .RespondToChallengeAsync(c.Id, false)
                        .ConfigureAwait(false);
                }
                else
                    okChallenges.Add(new Challenge(c, guestUser.Login, userId));
            }

            return Ok(okChallenges);
        }

        [HttpGet("accepted-challenges")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(Challenge), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<Challenge>> GetAcceptedChallengeAsync(
            [FromQuery] ulong userId, [FromQuery] DateTime challengeDate)
        {
            if (userId == 0)
                return BadRequest();

            var challenges = await _challengeRepository
                .GetChallengesByUserAndByDateAsync(userId, challengeDate.Date)
                .ConfigureAwait(false);

            Challenge okChallenge = null;
            foreach (var challenge in challenges.Where(c => c.IsAccepted > 0))
            {
                var opponentUser = await _userRepository
                    .GetUserByIdAsync(userId == challenge.HostUserId
                        ? challenge.GuestUserId
                        : challenge.HostUserId)
                    .ConfigureAwait(false);

                if (opponentUser == null || okChallenge != null)
                {
                    await _challengeRepository
                        .RespondToChallengeAsync(challenge.Id, false)
                        .ConfigureAwait(false);
                }
                else
                    okChallenge = new Challenge(challenge, opponentUser.Login, challenge.HostUserId);
            }

            return Ok(okChallenge);
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
                return BadRequest();

            var debut = ProposalChart.Default.FirstDate.Date;
            if (fromDate.HasValue && fromDate.Value.Date < debut)
                return BadRequest();

            var yesterday = _clock.Now.AddDays(-1).Date;
            if (toDate.HasValue && toDate.Value.Date < yesterday)
                return BadRequest();

            if (toDate.HasValue && fromDate.HasValue && toDate.Value.Date < fromDate.Value.Date)
                return BadRequest();

            var dateBeginOk = fromDate?.Date ?? debut;
            var dateEndOk = toDate?.Date ?? yesterday;

            var hostChallenges = await _challengeRepository
                .GetRequestedAcceptedChallengesAsync(userId, dateBeginOk, dateEndOk)
                .ConfigureAwait(false);

            var guestChallenges = await _challengeRepository
                .GetResponseAcceptedChallengesAsync(userId, dateBeginOk, dateEndOk)
                .ConfigureAwait(false);

            var challenges = new List<Challenge>(hostChallenges.Count + guestChallenges.Count);
            var usersCache = new Dictionary<ulong, string>();

            foreach (var c in hostChallenges)
            {
                var uLogin = await GetOrSetUserFromCacheAsync(
                        c.GuestUserId, usersCache)
                    .ConfigureAwait(false);
                challenges.Add(new Challenge(c, uLogin, userId));
            }

            foreach (var c in guestChallenges)
            {
                var uLogin = await GetOrSetUserFromCacheAsync(
                        c.HostUserId, usersCache)
                    .ConfigureAwait(false);
                challenges.Add(new Challenge(c, uLogin, userId));
            }

            return Ok(challenges.OrderByDescending(c => c.ChallengeDate).ToList());
        }

        private async Task<string> GetOrSetUserFromCacheAsync(ulong userId,
            Dictionary<ulong, string> usersCache)
        {
            if (!usersCache.ContainsKey(userId))
            {
                var user = await _userRepository
                    .GetUserByIdAsync(userId)
                    .ConfigureAwait(false);
                usersCache.Add(userId, user.Login);
            }
            return usersCache[userId];
        }
    }
}
