using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class LeaderController : KikoleBaseController
    {
        private readonly IBadgeService _badgeService;
        private readonly IChallengeRepository _challengeRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IUserRepository _userRepository;
        private readonly TextResources _resources;
        private readonly IClock _clock;

        public LeaderController(ILeaderRepository leaderRepository,
            IUserRepository userRepository,
            IPlayerRepository playerRepository,
            IProposalRepository proposalRepository,
            IChallengeRepository challengeRepository,
            IBadgeService badgeService,
            TextResources resources,
            IClock clock)
        {
            _leaderRepository = leaderRepository;
            _userRepository = userRepository;
            _playerRepository = playerRepository;
            _proposalRepository = proposalRepository;
            _challengeRepository = challengeRepository;
            _resources = resources;
            _badgeService = badgeService;
            _clock = clock;
        }

        [HttpGet("/recompute-badges")]
        [AuthenticationLevel(UserTypes.Administrator)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> ResetBadgesAsync()
        {
            await _badgeService
                .ResetBadgesAsync()
                .ConfigureAwait(false);

            return NoContent();
        }

        [HttpGet("day-leaders")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<Leader>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Leader>>> GetDayLeadersAsync(
            [FromQuery] DateTime day, [FromQuery] LeaderSorts sort)
        {
            if (sort == LeaderSorts.SuccessCount)
                return BadRequest(_resources.SuccessCountSortForbidden);

            var pDay = await _playerRepository
                .GetPlayerOfTheDayAsync(day)
                .ConfigureAwait(false);

            var leadersDto = await _leaderRepository
                .GetLeadersAtDateAsync(day)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var leaders = leadersDto
                .Select(dto => new Leader(dto, users))
                .ToList();

            var userCreator = users.SingleOrDefault(u => u.Id == pDay.CreationUserId);
            if (userCreator != null && pDay.HideCreator == 0)
                leaders.Add(new Leader(userCreator, day, leadersDto));

            /*var challenges = await _challengeRepository
                .GetAcceptedChallengesOfTheDayAsync(day)
                .ConfigureAwait(false);

            foreach (var challenge in challenges)
            {
                var hostLead = leadersDto.SingleOrDefault(l => l.UserId == challenge.HostUserId);
                var guestLead = leadersDto.SingleOrDefault(l => l.UserId == challenge.GuestUserId);
                var pointsDelta = Models.Challenge.ComputeHostPoints(challenge, hostLead, guestLead);
                if (pointsDelta != 0)
                {
                    if (hostLead == null)
                        leaders.Add(new Leader(challenge.HostUserId, pointsDelta, users));
                    else
                        leaders.Single(l => l.UserId == hostLead.UserId).WithPointsFromChallenge(pointsDelta, true);

                    if (guestLead == null)
                        leaders.Add(new Leader(challenge.GuestUserId, -pointsDelta, users));
                    else
                        leaders.Single(l => l.UserId == guestLead.UserId).WithPointsFromChallenge(pointsDelta, false);
                }
            }*/

            return Ok(Leader.DoubleSortWithPosition(leaders, sort));
        }

        [HttpGet("leaders")]
        [AuthenticationLevel]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Leader>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Leader>>> GetLeadersAsync(
            [FromQuery] DateTime? minimalDate,
            [FromQuery] DateTime? maximalDate,
            [FromQuery] LeaderSorts leaderSort,
            [FromQuery] bool includePvp)
        {
            if (!includePvp && !Enum.IsDefined(typeof(LeaderSorts), leaderSort))
                return BadRequest(_resources.InvalidSortType);

            if (minimalDate.HasValue && maximalDate.HasValue && minimalDate.Value.Date > maximalDate.Value.Date)
                return BadRequest(_resources.InvalidDateRange);

            var leaderDtos = await _leaderRepository
                .GetLeadersAsync(minimalDate, maximalDate)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            IReadOnlyCollection<Leader> leaders;
            if (includePvp)
            {
                leaderSort = LeaderSorts.TotalPoints;

                var challenges = await _challengeRepository
                    .GetAcceptedChallengesAsync(minimalDate, maximalDate)
                    .ConfigureAwait(false);

                leaders = ComputePvpLeaders(challenges, leaderDtos, users);
            }
            else
            {
                var players = await _playerRepository
                    .GetPlayersOfTheDayAsync(minimalDate, maximalDate)
                    .ConfigureAwait(false);

                leaders = ComputePveLeaders(leaderDtos, users, players);
            }

            return Ok(Leader.SortWithPosition(leaders, leaderSort));
        }

        [HttpGet("/awards")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(Awards), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<Awards>> GetAwardsOfTheMonthAsync(
            [FromQuery] DateTime date)
        {
            var awards = new Awards
            {
                Year = date.Year,
                Month = date.Month
            };

            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var leaderDtos = await _leaderRepository
                .GetLeadersAsync(firstDayOfMonth, lastDayOfMonth)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var players = await _playerRepository
                .GetPlayersOfTheDayAsync(firstDayOfMonth, lastDayOfMonth)
                .ConfigureAwait(false);

            var kikoles = await _leaderRepository
                .GetKikoleAwardsAsync(firstDayOfMonth, lastDayOfMonth)
                .ConfigureAwait(false);

            var leaders = ComputePveLeaders(leaderDtos, users, players);

            awards.PointsAwards = ComputeTopThreeLeadersAwards<PointsAward, int>(
                leaders,
                (a, l) => a.Points = l.TotalPoints,
                _ => _.Points,
                true);
            awards.TimeAwards = ComputeTopThreeLeadersAwards<TimeAward, TimeSpan>(
                leaders,
                (a, l) =>
                {
                    a.Time = l.BestTime;
                    a.PlayerName = players.Single(p =>
                        l.BestTimeDate.Date == p.ProposalDate.Value.Date).Name;
                },
                _ => _.Time,
                false);
            awards.CountAwards = ComputeTopThreeLeadersAwards<CountAward, int>(
                leaders,
                (a, l) => a.Count = l.SuccessCount,
                _ => _.Count,
                true);
            awards.HardestKikoles = GetTopThreeKikoles(kikoles, false);
            awards.EasiestKikoles = GetTopThreeKikoles(kikoles, true);

            return Ok(awards);
        }

        [HttpGet("/users/{userId}/stats")]
        [ProducesResponseType(typeof(UserStat), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<UserStat>> GetUserStatsAsync(ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources.InvalidUser);

            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return NotFound(_resources.UserDoesNotExist);

            var stats = new List<DailyUserStat>();

            var currentDate = await _playerRepository
                .GetFirstDateAsync()
                .ConfigureAwait(false);
            currentDate = currentDate.Date;

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

                var isCreator = (pDay.ProposalDate.Value.Date < now || pDay.HideCreator == 0)
                    && userId == pDay.CreationUserId;

                var singleStat = isCreator
                    ? new DailyUserStat(currentDate, pDay.Name, leaders)
                    : new DailyUserStat(userId, currentDate, pDay.Name, proposals.Count > 0, leaders, meLeader);

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
            var badges = await _badgeService
                .GetAllBadgesAsync()
                .ConfigureAwait(false);

            return Ok(badges);
        }

        [HttpGet("/users/{id}/badges")]
        [AuthenticationLevel]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IReadOnlyCollection<UserBadge>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<UserBadge>>> GetUserBadgesAsync(
            [FromRoute] ulong id, [FromQuery] ulong userId)
        {
            if (id == 0)
                return BadRequest(_resources.InvalidUser);

            var isAllowedToSeeHiddenBadge = userId == id;
            if (userId > 0 && userId != id)
            {
                var userDto = await _userRepository
                    .GetUserByIdAsync(userId)
                    .ConfigureAwait(false);

                isAllowedToSeeHiddenBadge = userDto?.UserTypeId == (ulong)UserTypes.Administrator;
            }

            var badgesFull = await _badgeService
                 .GetUserBadgesAsync(id, isAllowedToSeeHiddenBadge)
                 .ConfigureAwait(false);

            return Ok(badgesFull);
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

        private static IReadOnlyCollection<T> ComputeTopThreeLeadersAwards<T, TProp>(
            IReadOnlyCollection<Leader> leaders,
            Action<T, Leader> setAwardPropFunc,
            Func<T, TProp> getPosKeyFunc,
            bool descending)
            where T : BaseAward, new()
            where TProp : struct
        {
            return leaders
                .Select(_ =>
                {
                    var awd = new T
                    {
                        Name = _.Login
                    };
                    setAwardPropFunc(awd, _);
                    return awd;
                })
                .SetPositions(_ => getPosKeyFunc(_), descending, (_, x) => _.Position = x)
                .Where(_ => _.Position <= 3)
                .ToList();
        }

        private static List<KikoleAward> GetTopThreeKikoles(
            IReadOnlyCollection<KikoleAwardDto> kikoles, bool descending)
        {
            return kikoles
                .Select(_ => new KikoleAward
                {
                    AveragePoints = (int)Math.Round(_.AvgPts),
                    Name = _.Name
                })
                .SetPositions(_ => _.AveragePoints, descending, (_, x) => _.Position = x)
                .Where(_ => _.Position <= 3)
                .ToList();
        }

        private IReadOnlyCollection<Leader> ComputePvpLeaders(
            IReadOnlyCollection<ChallengeDto> challenges,
            IReadOnlyCollection<LeaderDto> leaderDtos,
            IReadOnlyCollection<UserDto> users)
        {
            var leaders = new List<Leader>();

            foreach (var challenge in challenges)
            {
                var hostLead = leaderDtos.SingleOrDefault(l => l.UserId == challenge.HostUserId && l.ProposalDate == challenge.ChallengeDate);
                var guestLead = leaderDtos.SingleOrDefault(l => l.UserId == challenge.GuestUserId && l.ProposalDate == challenge.ChallengeDate);
                var pointsDelta = Models.Challenge.ComputeHostPoints(challenge, hostLead, guestLead);
                if (pointsDelta != 0)
                {
                    leaders.Add(new Leader(challenge.HostUserId, pointsDelta + (hostLead?.Points ?? 0), users));
                    leaders.Add(new Leader(challenge.GuestUserId, -pointsDelta + (guestLead?.Points ?? 0), users));
                }
            }

            return leaders
                .GroupBy(l => l.UserId)
                .Select(l => new Leader(l))
                .ToList();
        }

        private IReadOnlyCollection<Leader> ComputePveLeaders(
            IReadOnlyCollection<LeaderDto> leaderDtos,
            IReadOnlyCollection<UserDto> users,
            IReadOnlyCollection<PlayerDto> players)
        {
            return leaderDtos
                .GroupBy(leaderDto => leaderDto.UserId)
                .Select(leaderDto => new Leader(leaderDto, users)
                    .WithPointsFromSubmittedPlayers(
                        GetDatesWithPlayerCreation(players, leaderDto), leaderDtos))
                .ToList();
        }

        private IEnumerable<DateTime> GetDatesWithPlayerCreation(IReadOnlyCollection<PlayerDto> players, IGrouping<ulong, LeaderDto> leaderDto)
        {
            return players
                .Where(p => p.CreationUserId == leaderDto.Key
                    && ((p.ProposalDate.Value < _clock.Now.Date) || p.HideCreator == 0))
                .Select(d => d.ProposalDate.Value);
        }
    }
}
