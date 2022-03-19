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

        [HttpGet("{userId}/badges")]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IReadOnlyCollection<UserBadge>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<UserBadge>>> GetUserBadgesAsync([FromRoute] ulong userId)
        {
            if (userId == 0)
                return BadRequest();

            var badges = await _badgeRepository
                .GetBadgesAsync()
                .ConfigureAwait(false);

            var uBadges = await _badgeRepository
                .GetUserBadgesAsync(userId)
                .ConfigureAwait(false);

            var badgesFull = uBadges
                .Select(b =>
                {
                    var bRef = badges.Single(_ => _.Id == b.BadgeId);
                    return new UserBadge
                    {
                        Description = bRef.Description,
                        GetDate = b.GetDate,
                        Badge = (Badges)bRef.Id,
                        Name = bRef.Name,
                        Users = bRef.Users
                    };
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

        // TODO: change password
        // TODO: reset password with q&a

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

            var currentDate = ProposalChart.Default.FirstDate.Date;
            while (currentDate <= _clock.Now.Date)
            {
                var leaders = await _leaderRepository
                    .GetLeadersAtDateAsync(currentDate)
                    .ConfigureAwait(false);

                var playerOfTheDay = await _playerRepository
                    .GetPlayerOfTheDayAsync(currentDate)
                    .ConfigureAwait(false);

                if (playerOfTheDay.YearOfBirth < 1970 && badges.Contains(Badges.Archaeology))
                {
                    await CheckBadgeInternalAsync(
                            Badges.Archaeology, currentDate, leaders, l => true)
                        .ConfigureAwait(false);
                }

                if (playerOfTheDay.YearOfBirth < 1940 && badges.Contains(Badges.WorldWarTwo))
                {
                    await CheckBadgeInternalAsync(
                            Badges.WorldWarTwo, currentDate, leaders, l => true)
                        .ConfigureAwait(false);
                }

                if (playerOfTheDay.BadgeId.HasValue)
                {
                    await CheckBadgeInternalAsync(
                            (Badges)playerOfTheDay.BadgeId.Value, currentDate, leaders, l => true)
                        .ConfigureAwait(false);
                }
                
                var oldLeaders = new List<IReadOnlyCollection<LeaderDto>>();
                for (var i = 1; i <= 29; i++)
                {
                    var leadersBefore = await _leaderRepository
                        .GetLeadersAtDateAsync(currentDate.AddDays(-i))
                        .ConfigureAwait(false);
                    oldLeaders.Add(leadersBefore);
                }

                var badgeCondition = new Dictionary<Badges, Func<LeaderDto, bool>>
                {
                    {
                        Badges.CacaCaféClopeKikolé,
                        l => new TimeSpan(0, l.Time, 0).Hours >= 5 && new TimeSpan(0, l.Time, 0).Hours < 8
                    },
                    {
                        Badges.HalfwayToTheTop,
                        l => l.Points >= 500
                    },
                    {
                        Badges.ItsOver900,
                        l => l.Points >= 900
                    },
                    {
                        Badges.SavedByTheBell,
                        l => new TimeSpan(0, l.Time, 0).Hours == 23
                    },
                    {
                        Badges.StayUpLate,
                        l => new TimeSpan(0, l.Time, 0).Hours < 2
                    },
                    {
                        Badges.YourActualFirstSuccess,
                        l => l.Points > 0
                    },
                    {
                        Badges.YourFirstSuccess,
                        l => true
                    },
                    {
                        Badges.OverTheTopPart1,
                        l => l.Time == leaders.Min(_ => _.Time)
                    },
                    {
                        Badges.OverTheTopPart2,
                        l => l.Points == leaders.Min(_ => _.Points)
                    },
                    {
                        Badges.ThreeInARow,
                        l => oldLeaders.Take(2).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                    },
                    {
                        Badges.AWeekInARow,
                        l => oldLeaders.Take(6).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                    },
                    {
                        Badges.LegendTier,
                        l => oldLeaders.Count >= 29 && oldLeaders.Take(29).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                    }
                };

                foreach (var badge in badges.Where(b => badgeCondition.ContainsKey(b)))
                {
                    await CheckBadgeInternalAsync(
                            badge, currentDate, leaders, l => badgeCondition[badge](l))
                        .ConfigureAwait(false);
                }

                currentDate = currentDate.AddDays(1).Date;
            }

            return NoContent();
        }

        private async Task CheckBadgeInternalAsync(Badges badge,
            DateTime currentDate, IReadOnlyCollection<LeaderDto> leaders,
            Func<LeaderDto, bool> conditionToCheck)
        {
            var usersWithbadge = await _badgeRepository
                .GetUsersWithBadgeAsync((ulong)badge)
                .ConfigureAwait(false);

            foreach (var leader in leaders.Where(l =>
                conditionToCheck(l)
                && !usersWithbadge.Any(u => u.UserId == l.UserId)))
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
