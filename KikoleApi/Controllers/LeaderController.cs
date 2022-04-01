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
        private readonly IChallengeRepository _challengeRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IClubRepository _clubRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IClock _clock;

        public LeaderController(ILeaderRepository leaderRepository,
            IUserRepository userRepository,
            IPlayerRepository playerRepository,
            IClubRepository clubRepository,
            IProposalRepository proposalRepository,
            IChallengeRepository challengeRepository,
            IClock clock)
        {
            _leaderRepository = leaderRepository;
            _userRepository = userRepository;
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
            _proposalRepository = proposalRepository;
            _challengeRepository = challengeRepository;
            _clock = clock;
        }

        [HttpGet("day-leaders")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<Leader>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Leader>>> GetDayLeadersAsync(
            [FromQuery] DateTime day, [FromQuery] LeaderSorts sort)
        {
            if (sort == LeaderSorts.SuccessCount)
                return BadRequest();

            var leadersDto = await _leaderRepository
                .GetLeadersAtDateAsync(day)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var leaders = leadersDto
                .Select(dto => new Leader(dto, users))
                .ToList();

            var challenges = await _challengeRepository
                .GetAcceptedChallengesOfTheDayAsync(day)
                .ConfigureAwait(false);

            /*foreach (var challenge in challenges)
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

        [HttpPut("leaders-computing")]
        [AuthenticationLevel(UserTypes.Administrator)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> RecomputeLeadersAsync()
        {
            var players = await _playerRepository
                .GetProposedPlayersAsync()
                .ConfigureAwait(false);

            foreach (var playerOfTheDay in players)
            {
                var playerClubs = await _playerRepository
                    .GetPlayerClubsAsync(playerOfTheDay.Id)
                    .ConfigureAwait(false);

                var playerClubsDetails = new List<ClubDto>(playerClubs.Count);
                foreach (var pc in playerClubs)
                {
                    var c = await _clubRepository
                        .GetClubAsync(pc.ClubId)
                        .ConfigureAwait(false);
                    playerClubsDetails.Add(c);
                }

                await _leaderRepository
                    .DeleteLeadersAsync(playerOfTheDay.ProposalDate.Value)
                    .ConfigureAwait(false);

                var proposalUsers = await _proposalRepository
                    .GetWiningProposalsAsync(playerOfTheDay.ProposalDate.Value)
                    .ConfigureAwait(false);

                var leaders = proposalUsers
                    .Select(p => p.UserId)
                    .Distinct();

                foreach (var userId in leaders)
                {
                    var proposals = await _proposalRepository
                        .GetProposalsAsync(playerOfTheDay.ProposalDate.Value, userId)
                        .ConfigureAwait(false);

                    var points = ProposalChart.Default.BasePoints;

                    foreach (var proposal in proposals.Where(p => p.DaysBefore == 0).OrderBy(p => p.CreationDate))
                    {
                        var minusPoints = ProposalChart.Default.ProposalTypesCost[(ProposalTypes)proposal.ProposalTypeId];
                        if (proposal.Successful == 0)
                            points -= minusPoints;

                        if (proposal.Successful > 0 && (ProposalTypes)proposal.ProposalTypeId == ProposalTypes.Name)
                        {
                            await _leaderRepository
                                .CreateLeaderAsync(new LeaderDto
                                {
                                    Points = (ushort)(points < 0 ? 0 : points),
                                    ProposalDate = proposal.ProposalDate,
                                    Time = Convert.ToUInt16(Math.Ceiling(
                                        (proposal.CreationDate - playerOfTheDay.ProposalDate.Value.Date).TotalMinutes)),
                                    UserId = proposal.UserId
                                })
                                .ConfigureAwait(false);
                            break;
                        }
                    }
                }
            }

            return NoContent();
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
                return BadRequest();

            if (minimalDate.HasValue && maximalDate.HasValue && minimalDate.Value.Date > maximalDate.Value.Date)
                return BadRequest();

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

        private static IReadOnlyCollection<Leader> ComputePveLeaders(
            IReadOnlyCollection<LeaderDto> leaderDtos,
            IReadOnlyCollection<UserDto> users,
            IReadOnlyCollection<PlayerDto> players)
        {
            return leaderDtos
                .GroupBy(leaderDto => leaderDto.UserId)
                .Select(leaderDto => new Leader(leaderDto, users)
                    .WithPointsFromSubmittedPlayers(
                        players.Where(p => p.CreationUserId == leaderDto.Key).Select(d => d.ProposalDate.Value),
                        leaderDtos))
                .ToList();
        }
    }
}
