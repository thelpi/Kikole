﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class LeaderController : KikoleBaseController
    {
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
            IClock clock)
        {
            _leaderRepository = leaderRepository;
            _userRepository = userRepository;
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
            _proposalRepository = proposalRepository;
            _clock = clock;
        }

        [HttpGet("day-leaders")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<Leader>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Leader>>> GetDayLeadersAsync([FromQuery] DateTime day)
        {
            var dtos = await _leaderRepository
                .GetLeadersAtDateAsync(day.Date)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var leaders = dtos
                .GroupBy(dto => dto.UserId)
                .Select(dto => new Leader(dto, users))
                .ToList();

            leaders = leaders
                .OrderBy(l => l.BestTime.TotalMinutes)
                .ThenByDescending(l => l.TotalPoints)
                .Select((l, i) =>
                {
                    l.Position = i + 1;
                    return l;
                })
                .ToList();

            return Ok(leaders);
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
            [FromQuery] LeaderSorts leaderSort)
        {
            if (!Enum.IsDefined(typeof(LeaderSorts), leaderSort))
                return BadRequest();

            if (minimalDate.HasValue && maximalDate.HasValue && minimalDate.Value.Date > maximalDate.Value.Date)
                return BadRequest();

            if (minimalDate.HasValue && minimalDate.Value.Date > _clock.Now.Date)
                return BadRequest();

            if (maximalDate.HasValue && maximalDate.Value.Date > _clock.Now.Date)
                return BadRequest();

            var leaderDtos = await _leaderRepository
                .GetLeadersAsync(minimalDate, maximalDate)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var players = await _playerRepository
                .GetPlayersOfTheDayAsync(minimalDate, maximalDate)
                .ConfigureAwait(false);

            var leaders = leaderDtos
                .GroupBy(leaderDto => leaderDto.UserId)
                .Select(leaderDto => new Leader(leaderDto, users)
                    .WithPointsFromSubmittedPlayers(
                        players.Where(p => p.CreationUserId == leaderDto.Key).Select(d => d.ProposalDate.Value),
                        leaderDtos))
                .ToList();
            
            switch (leaderSort)
            {
                case LeaderSorts.SuccessCount:
                    leaders = leaders.SetPositions(l => l.SuccessCount, true, (l, i) => l.Position = i);
                    break;
                case LeaderSorts.BestTime:
                    leaders = leaders.SetPositions(l => l.BestTime, false, (l, i) => l.Position = i);
                    break;
                case LeaderSorts.TotalPoints:
                    leaders = leaders.SetPositions(l => l.TotalPoints, true, (l, i) => l.Position = i);
                    break;
            }

            return Ok(leaders);
        }
    }
}
