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
        private readonly IBadgeRepository _badgeRepository;
        private readonly IChallengeRepository _challengeRepository;
        private readonly IClock _clock;
        private readonly IPlayerRepository _playerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILeaderRepository _leaderRepository;

        public ChallengeController(IChallengeRepository challengeRepository,
            IPlayerRepository playerRepository,
            IUserRepository userRepository,
            ILeaderRepository leaderRepository,
            IBadgeRepository badgeRepository,
            IClock clock)
        {
            _challengeRepository = challengeRepository;
            _playerRepository = playerRepository;
            _userRepository = userRepository;
            _leaderRepository = leaderRepository;
            _badgeRepository = badgeRepository;
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

            var challengeAlready = await _challengeRepository
                .GetUsersFutureChallengesAsync(userId, request.GuestUserId)
                .ConfigureAwait(false);

            if (challengeAlready.Count > 0)
                return Conflict("Can't create challenge; a challenge against this opponent is already planned or requested");

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

            if (isCancel && isAccepted)
                return Forbid();

            if (isCancel && challenge.IsAccepted.HasValue)
                return Conflict("You can't cancel an accepted challenge");

            if (!isCancel && challenge.IsAccepted.HasValue)
                return Conflict("You've already respond to this challenge");

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

                return Conflict("Opponent is an invalid account");
            }

            var hostDates = await _challengeRepository
                .GetBookedChallengesAsync(userId)
                .ConfigureAwait(false);

            var guestDates = await _challengeRepository
                .GetBookedChallengesAsync(challenge.GuestUserId)
                .ConfigureAwait(false);

            var challengeDate = _clock.Now.Date;
            Models.Dtos.PlayerDto p;
            do
            {
                challengeDate = challengeDate.AddDays(1);

                p = await _playerRepository
                    .GetPlayerOfTheDayAsync(challengeDate)
                    .ConfigureAwait(false);
            }
            while (hostDates.Contains(challengeDate)
                || guestDates.Contains(challengeDate)
                || p.CreationUserId == challenge.GuestUserId
                || p.CreationUserId == challenge.HostUserId);

            await _challengeRepository
                .RespondToChallengeAsync(id, isAccepted, challengeDate)
                .ConfigureAwait(false);

            if (isAccepted)
            {
                await AddBadgesIfEligibleAsync(
                        Badges.ChallengeAccepted, challenge)
                    .ConfigureAwait(false);

                if (challenge.PointsRate >= 80)
                {
                    await AddBadgesIfEligibleAsync(
                           Badges.AllIn, challenge)
                       .ConfigureAwait(false);
                }

                var allAccepted = await _challengeRepository
                    .GetAcceptedChallengesAsync(null, null)
                    .ConfigureAwait(false);
                
                if (allAccepted.Count(ac => ac.HostUserId == challenge.HostUserId) >= 5)
                {
                    var usersBadged = await _badgeRepository
                       .GetUsersWithBadgeAsync((ulong)Badges.GambleAddiction)
                       .ConfigureAwait(false);

                    await AddBadgeIfEligibleAsync(
                            challenge, usersBadged, Badges.GambleAddiction, c => c.HostUserId)
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
                return BadRequest();

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
                return BadRequest();

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
                return BadRequest();

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

        private async Task AddBadgesIfEligibleAsync(Badges badge, Models.Dtos.ChallengeDto challenge)
        {
            var usersBadged = await _badgeRepository
                   .GetUsersWithBadgeAsync((ulong)badge)
                   .ConfigureAwait(false);

            await AddBadgeIfEligibleAsync(
                    challenge, usersBadged, badge, c => c.GuestUserId)
                .ConfigureAwait(false);
            await AddBadgeIfEligibleAsync(
                    challenge, usersBadged, badge, c => c.HostUserId)
                .ConfigureAwait(false);
        }

        private async Task AddBadgeIfEligibleAsync(Models.Dtos.ChallengeDto challenge,
            IEnumerable<Models.Dtos.UserBadgeDto> usersBadged,
            Badges badge,
            Func<Models.Dtos.ChallengeDto, ulong> userFunc)
        {
            if (!usersBadged.Any(ub => ub.UserId == userFunc(challenge)))
            {
                await _badgeRepository
                    .InsertUserBadgeAsync(new Models.Dtos.UserBadgeDto
                    {
                        GetDate = _clock.Now.Date,
                        BadgeId = (ulong)badge,
                        UserId = userFunc(challenge)
                    })
                    .ConfigureAwait(false);
            }
        }
    }
}
