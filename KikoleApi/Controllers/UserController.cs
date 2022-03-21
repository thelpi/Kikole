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
        private readonly IBadgeRepository _badgeRepository;
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
            IBadgeRepository badgeRepository,
            IClock clock)
        {
            _userRepository = userRepository;
            _badgeRepository = badgeRepository;
            _proposalRepository = proposalRepository;
            _leaderRepository = leaderRepository;
            _playerRepository = playerRepository;
            _clock = clock;
            _crypter = crypter;
        }

        [HttpGet("known-players")]
        [AuthenticationLevel(AuthenticationLevel.Authenticated)]
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

        [HttpGet("{userId}/badges")]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IReadOnlyCollection<UserBadge>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<UserBadge>>> GetUserBadgesAsync([FromRoute] ulong userId)
        {
            if (userId == 0)
                return BadRequest();

            var badges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            var uBadges = await _badgeRepository
                .GetUserBadgesAsync(userId)
                .ConfigureAwait(false);

            var badgesFull = uBadges
                .Select(b =>
                {
                    return new UserBadge(
                        badges.Single(_ => _.Id == b.BadgeId),
                        b);
                })
                .OrderByDescending(b => b.GetDate)
                .ToList();

            return Ok(badgesFull);
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

        [HttpPut("/user-passwords")]
        [AuthenticationLevel(AuthenticationLevel.Authenticated)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ChangePasswordAsync([FromQuery] ulong userId,
            [FromQuery] string oldp, [FromQuery] string newp)
        {
            if (userId == 0 || string.IsNullOrWhiteSpace(oldp) || string.IsNullOrWhiteSpace(newp))
                return BadRequest();

            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return NotFound();

            var success = await _userRepository
                .ResetUserKnownPasswordAsync(
                    user.Login,
                    _crypter.Encrypt(oldp),
                    _crypter.Encrypt(newp))
                .ConfigureAwait(false);

            if (!success)
                return Forbid();

            return NoContent();
        }

        [HttpPatch("/user-questions")]
        [AuthenticationLevel(AuthenticationLevel.Authenticated)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateUserQAndA([FromQuery] ulong userId,
            [FromQuery] string question,
            [FromQuery] string answer)
        {
            if (userId == 0
                || string.IsNullOrWhiteSpace(question)
                || string.IsNullOrWhiteSpace(answer))
                return BadRequest();

            await _userRepository
                .ResetUserQAndAAsync(userId, question, _crypter.Encrypt(answer))
                .ConfigureAwait(false);

            return NoContent();
        }

        [HttpPatch("/reset-passwords")]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ResetPasswordAsync([FromQuery] string login,
            [FromQuery] string answer, [FromQuery] string newPassword)
        {
            if (string.IsNullOrWhiteSpace(login)
                || string.IsNullOrWhiteSpace(answer)
                || string.IsNullOrWhiteSpace(newPassword))
                return BadRequest();

            var response = await _userRepository
                .ResetUserUnknownPasswordAsync(
                    login,
                    _crypter.Encrypt(answer),
                    _crypter.Encrypt(newPassword))
                .ConfigureAwait(false);

            if (!response)
                return Forbid();

            return NoContent();
        }

        [HttpGet("{userId}/stats")]
        [ProducesResponseType(typeof(UserStat), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<UserStat>> GetUserStatsAsync(ulong userId)
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

        [HttpGet("/badges")]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Badge>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Badge>>> GetBadgesAsync()
        {
            var dtos = await _badgeRepository
                .GetBadgesAsync(false)
                .ConfigureAwait(false);

            var badges = dtos
                .Select(b => new Badge(b))
                .OrderByDescending(b => b.Users)
                .ToList();

            return Ok(badges);
        }

        [HttpPut("/badges")]
        [AuthenticationLevel(AuthenticationLevel.AdminAuthenticated)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> RecomputeBadgesAsync([FromQuery] Badges[] badges)
        {
            badges = badges?.Length > 0
                ? badges
                : Enum.GetValues(typeof(Badges)).Cast<Badges>().ToArray();

            foreach (var badge in badges)
            {
                await _badgeRepository
                    .ResetBadgeDatasAsync((ulong)badge)
                    .ConfigureAwait(false);
            }

            var playersCache = new Dictionary<DateTime, PlayerDto>();
            var leadersHistory = new List<IReadOnlyCollection<LeaderDto>>();

            var currentDate = ProposalChart.Default.FirstDate.Date;
            while (currentDate <= _clock.Now.Date)
            {
                var leaders = await _leaderRepository
                    .GetLeadersAtDateAsync(currentDate)
                    .ConfigureAwait(false);

                var playerOfTheDay = await GetPlayerOfTheDayFromCacheAsync(
                        playersCache, currentDate)
                    .ConfigureAwait(false);

                foreach (var badge in badges.Where(b => BadgeHelper.PlayerBasedBadgeCondition.ContainsKey(b)))
                {
                    await CheckBadgeInternalAsync(
                            badge, currentDate, leaders, leadersHistory, (l, lh) => BadgeHelper.PlayerBasedBadgeCondition[badge](playerOfTheDay))
                        .ConfigureAwait(false);
                }

                foreach (var badge in badges.Where(b => BadgeHelper.LeadersBasedBadgeCondition.ContainsKey(b)))
                {
                    await CheckBadgeInternalAsync(
                            badge, currentDate, leaders, leadersHistory, (l, lh) => BadgeHelper.LeadersBasedBadgeCondition[badge](l, leaders))
                        .ConfigureAwait(false);
                }

                foreach (var badge in badges.Where(b => BadgeHelper.LeaderBasedBadgeCondition.ContainsKey(b)))
                {
                    await CheckBadgeInternalAsync(
                            badge, currentDate, leaders, leadersHistory, BadgeHelper.LeaderBasedBadgeCondition[badge])
                        .ConfigureAwait(false);
                }

                foreach (var leader in leaders)
                {
                    var myHistory = leadersHistory
                        .SelectMany(lh => lh)
                        .Where(lh => lh.UserId == leader.UserId)
                        .ToList();

                    var playersHistory = new List<PlayerDto>();
                    foreach (var mh in myHistory)
                    {
                        var p = await GetPlayerOfTheDayFromCacheAsync(
                                playersCache, mh.ProposalDate.Date)
                            .ConfigureAwait(false);
                        playersHistory.Add(p);
                    }
                    playersHistory.Add(playerOfTheDay);

                    foreach (var badge in badges.Where(b => BadgeHelper.PlayersHistoryBadgeCondition.ContainsKey(b)))
                    {
                        var hasBadge = await _badgeRepository
                            .CheckUserHasBadgeAsync(leader.UserId, (ulong)badge)
                            .ConfigureAwait(false);

                        if (!hasBadge && BadgeHelper.PlayersHistoryBadgeCondition[badge](playersHistory))
                        {
                            await _badgeRepository
                                .InsertUserBadgeAsync(new UserBadgeDto
                                {
                                    GetDate = currentDate,
                                    BadgeId = (ulong)badge,
                                    UserId = leader.UserId
                                })
                                .ConfigureAwait(false);
                        }
                    }
                }

                leadersHistory.Insert(0, leaders);
                currentDate = currentDate.AddDays(1).Date;
            }

            return NoContent();
        }

        [HttpGet("/admin-users")]
        [AuthenticationLevel(AuthenticationLevel.Authenticated)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAdministratorUserAsync([FromQuery] ulong userId)
        {
            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null || user.IsAdmin != 1)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{login}/questions")]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<string>> GetLoginQuestionAsync([FromRoute] string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return BadRequest();

            var user = await _userRepository
                .GetUserByLoginAsync(login)
                .ConfigureAwait(false);

            if (user == null)
                return NotFound();

            return Ok(user.PasswordResetQuestion);
        }

        private async Task<PlayerDto> GetPlayerOfTheDayFromCacheAsync(Dictionary<DateTime, PlayerDto> playersCache, DateTime currentDate)
        {
            if (playersCache.ContainsKey(currentDate))
            {
                return playersCache[currentDate];
            }

            var playerOfTheDay = await _playerRepository
                .GetPlayerOfTheDayAsync(currentDate)
                .ConfigureAwait(false);
            playersCache.Add(currentDate, playerOfTheDay);
            return playerOfTheDay;
        }

        private async Task CheckBadgeInternalAsync(Badges badge,
            DateTime currentDate, IReadOnlyCollection<LeaderDto> leaders,
            IReadOnlyCollection<IReadOnlyCollection<LeaderDto>> leadersHistory,
            Func<LeaderDto, IReadOnlyCollection<IReadOnlyCollection<LeaderDto>>, bool> conditionToCheck)
        {
            foreach (var leader in leaders.Where(l => conditionToCheck(l, leadersHistory)))
            {
                var hasBadge = await _badgeRepository
                    .CheckUserHasBadgeAsync(leader.UserId, (ulong)badge)
                    .ConfigureAwait(false);

                if (!hasBadge)
                {
                    await _badgeRepository
                        .InsertUserBadgeAsync(new UserBadgeDto
                        {
                            GetDate = currentDate,
                            BadgeId = (ulong)badge,
                            UserId = leader.UserId
                        })
                        .ConfigureAwait(false);
                }
            }
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
