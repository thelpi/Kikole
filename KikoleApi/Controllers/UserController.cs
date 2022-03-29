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
using KikoleApi.Models.Enums;
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

        [HttpGet]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<User>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<User>>> GetUsersAsync()
        {
            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            return Ok(users.Select(u => new User(u)).ToList());
        }

        [HttpGet("/player-of-the-day-users")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(PlayerCreator), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<ActionResult<PlayerCreator>> GetPlayerOfTheDayFromUserAsync(
            [FromQuery] ulong userId, [FromQuery] DateTime proposalDate)
        {
            var p = await _playerRepository
                .GetPlayerOfTheDayAsync(proposalDate.Date)
                .ConfigureAwait(false);

            var u = await _userRepository
                .GetUserByIdAsync(p.CreationUserId)
                .ConfigureAwait(false);

            return base.Ok(new PlayerCreator(userId, p, u));
        }

        [HttpGet("known-players")]
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

        [HttpGet("{id}/badges")]
        [AuthenticationLevel]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IReadOnlyCollection<UserBadge>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<UserBadge>>> GetUserBadgesAsync(
            [FromRoute] ulong id, [FromQuery] ulong userId)
        {
            if (id == 0)
                return BadRequest();

            var isAllowedToSeeHiddenBadge = userId == id;
            if (userId > 0 && userId != id)
            {
                var userDto = await _userRepository
                    .GetUserByIdAsync(userId)
                    .ConfigureAwait(false);

                isAllowedToSeeHiddenBadge = userDto?.UserTypeId == (ulong)UserTypes.Administrator;
            }

            var badges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            var dtos = await _badgeRepository
                .GetUserBadgesAsync(id)
                .ConfigureAwait(false);

            var badgesFull = new List<UserBadge>();
            foreach (var dto in dtos)
            {
                var b = badges.Single(_ => _.Id == dto.BadgeId);

                if (_clock.Now.Date == dto.GetDate
                    && b.Hidden > 0
                    && !isAllowedToSeeHiddenBadge)
                {
                    continue;
                }

                var users = await _badgeRepository
                    .GetUsersWithBadgeAsync(dto.BadgeId)
                    .ConfigureAwait(false);

                badgesFull.Add(new UserBadge(b, dto, users.Count));
            }

            badgesFull = badgesFull
                .OrderByDescending(b => b.Hidden)
                .ThenBy(b => b.Users)
                .ToList();

            return Ok(badgesFull);
        }

        [HttpPost]
        [AuthenticationLevel]
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
        [AuthenticationLevel]
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

            var value = $"{existingUser.Id}_{existingUser.UserTypeId}";

            return $"{value}_{_crypter.Encrypt(value)}";
        }

        [HttpPut("/user-passwords")]
        [AuthenticationLevel(UserTypes.StandardUser)]
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
        [AuthenticationLevel(UserTypes.StandardUser)]
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
        [AuthenticationLevel]
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

                var singleStat = new DailyUserStat(userId, currentDate, pDay.Name,
                    proposals.Count > 0, leaders, meLeader);

                stats.Add(singleStat);
                currentDate = currentDate.AddDays(1);
            }

            return Ok(new UserStat(stats, user.Login));
        }

        [HttpGet("/badges")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<Badge>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Badge>>> GetBadgesAsync()
        {
            var dtos = await _badgeRepository
                .GetBadgesAsync(false)
                .ConfigureAwait(false);

            var badges = new List<Badge>(dtos.Count);
            foreach (var dto in dtos)
            {
                var users = await _badgeRepository
                   .GetUsersWithBadgeAsync(dto.Id)
                   .ConfigureAwait(false);

                badges.Add(new Badge(dto, users.Count));
            }

            badges = badges
                .OrderByDescending(b => b.Users)
                .ToList();

            return Ok(badges);
        }

        [HttpPut("/badges")]
        [AuthenticationLevel(UserTypes.Administrator)]
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

        [HttpGet("/user-types")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<UserTypes>> GetUserAsync([FromQuery] ulong userId)
        {
            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return NotFound();

            return Ok((UserTypes)user.UserTypeId);
        }

        [HttpGet("{login}/questions")]
        [AuthenticationLevel]
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
    }
}
