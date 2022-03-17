using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    [Route("users")]
    public class UserController : KikoleBaseController
    {
        private readonly IUserRepository _userRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly ICrypter _crypter;
        private readonly IClock _clock;

        public UserController(IUserRepository userRepository,
            IProposalRepository proposalRepository,
            ILeaderRepository leaderRepository,
            ICrypter crypter,
            IPlayerRepository playerRepository,
            IClock clock)
        {
            _userRepository = userRepository;
            _proposalRepository = proposalRepository;
            _leaderRepository = leaderRepository;
            _playerRepository = playerRepository;
            _clock = clock;
            _crypter = crypter;
        }

        [HttpPost]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> CreateUserAsync([FromBody] UserRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request: null");

            var validityRequest = request.IsValid();
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest($"Invalid request: {validityRequest}");
            
            var existingUser = await _userRepository
                .GetUserByLoginAsync(request.Login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser != null)
                return Conflict("A account already exists with this login");

            var userId = await _userRepository
                .CreateUserAsync(request.ToDto(_crypter))
                .ConfigureAwait(false);

            if (userId == 0)
                return StatusCode((int)HttpStatusCode.InternalServerError, "User creation failure");

            return Created($"users/{userId}", null);
        }

        [HttpGet("{login}/authentication-tokens")]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<string>> GetAuthenticationTokenAsync(
            [FromRoute] string login,
            [FromQuery][Required] string password)
        {
            if (string.IsNullOrWhiteSpace(login))
                return BadRequest("Invalid request: empty login");

            if (string.IsNullOrWhiteSpace(password))
                return BadRequest("Invalid request: empty password");

            var existingUser = await _userRepository
                .GetUserByLoginAsync(login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser == null)
                return NotFound();

            if (!_crypter.Encrypt(password).Equals(existingUser.Password))
                return Unauthorized();

            var encryptedCookiePart = _crypter.Encrypt($"{existingUser.Id}_{existingUser.IsAdmin}");

            return $"{existingUser.Id}_{existingUser.IsAdmin}_{encryptedCookiePart}";
        }

        // TODO: change password
        // TODO: reset password with q&a

        [HttpGet("{userId}/stats")]
        [ProducesResponseType(typeof(UserStat), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<UserStat>> GetUserStats(ulong userId)
        {
            if (userId == 0)
                return BadRequest();
            
            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return NotFound();

            var stats = new List<DailyUserStat>();

            var currentDate = ProposalChart.Default.FirstDate.Date;
            var now = _clock.Now.Date;
            while (currentDate <= now)
            {
                var pDay = await _playerRepository
                    .GetPlayerOfTheDayAsync(currentDate)
                    .ConfigureAwait(false);

                var proposals = await _proposalRepository
                    .GetProposalsDateExactAsync(currentDate, userId)
                    .ConfigureAwait(false);

                var leaders = await _leaderRepository
                    .GetLeadersAtDateAsync(currentDate)
                    .ConfigureAwait(false);

                var meLeader = leaders.SingleOrDefault(l => l.UserId == userId);

                var singleStat = new DailyUserStat
                {
                    Date = currentDate,
                    Answer = pDay.Name,
                    Attempt = proposals.Count > 0,
                    Points = meLeader != null
                        ? meLeader.Points
                        : default(int?),
                    Time = meLeader != null
                        ? new TimeSpan(0, meLeader.Time, 0)
                        : default(TimeSpan?),
                    PointsPosition = GetUserPositionInLeaders(userId,
                        leaders.OrderByDescending(t => t.Points)),
                    TimePosition = GetUserPositionInLeaders(userId,
                        leaders.OrderBy(t => t.Time))
                };

                stats.Add(singleStat);
                currentDate = currentDate.AddDays(1);
            }

            var us = new UserStat
            {
                Attempts = stats.Count(s => s.Attempt),
                AverageTime = stats.Where(s => s.Time.HasValue).Select(s => s.Time.Value).Average(),
                BestPoints = stats.Any(s => s.Points.HasValue)
                    ? stats.Where(s => s.Points.HasValue).Max(s => s.Points.Value)
                    : default(int?),
                BestTime = stats.Any(s => s.Time.HasValue)
                    ? stats.Where(s => s.Time.HasValue).Min(s => s.Time.Value)
                    : default(TimeSpan?),
                Login = user.Login,
                Stats = stats,
                Successes = stats.Count(s => s.Points.HasValue),
                TotalPoints = stats.Sum(s => s.Points.GetValueOrDefault(0))
            };

            return Ok(us);
        }

        private static int? GetUserPositionInLeaders(ulong userId,
            IOrderedEnumerable<LeaderDto> orderedLeaders)
        {
            var tIndex = -1;
            var i = 0;
            foreach (var orderedLeader in orderedLeaders)
            {
                if (orderedLeader.UserId == userId)
                {
                    tIndex = i + 1;
                    break;
                }
                i++;
            }

            return tIndex == -1
                ? default(int?)
                : tIndex;
        }
    }
}
