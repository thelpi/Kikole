﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
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

        public LeaderController(ILeaderRepository leaderRepository,
            IUserRepository userRepository,
            IPlayerRepository playerRepository,
            IClubRepository clubRepository,
            IProposalRepository proposalRepository)
        {
            _leaderRepository = leaderRepository;
            _userRepository = userRepository;
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
            _proposalRepository = proposalRepository;
        }

        [HttpPut("leaders-computing")]
        [AuthenticationLevel(AuthenticationLevel.AdminAuthenticated)]
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

                IEnumerable<(ulong userId, string ip)> leaders = proposalUsers
                    .Select(p => p.UserId > 0
                        ? (p.UserId.Value, p.Ip)
                        : (0, p.Ip))
                    .Distinct();

                foreach (var (userId, ip) in leaders)
                {
                    var proposals = await _proposalRepository
                        .GetProposalsAsync(playerOfTheDay.ProposalDate.Value, userId, ip)
                        .ConfigureAwait(false);

                    var points = ProposalChart.Default.BasePoints;

                    foreach (var proposal in proposals.Where(p => p.DaysBefore == 0).OrderBy(p => p.CreationDate))
                    {
                        var minusPoints = ProposalChart.Default.ProposalTypesCost[(ProposalType)proposal.ProposalTypeId];
                        if (proposal.Successful == 0 || (ProposalType)proposal.ProposalTypeId == ProposalType.Clue)
                            points -= minusPoints;

                        if (proposal.Successful > 0 && (ProposalType)proposal.ProposalTypeId == ProposalType.Name)
                        {
                            await _leaderRepository
                                .CreateLeaderAsync(new LeaderDto
                                {
                                    Ip = proposal.Ip,
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
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Leader>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Leader>>> GetLeadersAsync(
            [FromQuery] DateTime? minimalDate,
            [FromQuery] bool includeAnonymous,
            [FromQuery] LeaderSort leaderSort)
        {
            var dtos = await _leaderRepository
                .GetLeadersAsync(minimalDate, includeAnonymous)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var leaders = dtos
                .GroupBy(dto => dto.Key)
                .Select(dto => new Leader(dto, users))
                .ToList();

            switch (leaderSort)
            {
                case LeaderSort.SuccessCount:
                    leaders = leaders.OrderByDescending(l => l.SuccessCount).ToList();
                    break;
                case LeaderSort.BestTime:
                    leaders = leaders.OrderBy(l => l.BestTime.TotalMinutes).ToList();
                    break;
                case LeaderSort.TotalPoints:
                    leaders = leaders.OrderByDescending(l => l.TotalPoints).ToList();
                    break;
            }

            return Ok(leaders);
        }
    }
}
