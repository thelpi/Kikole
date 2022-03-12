using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class LeaderController : KikoleBaseController
    {
        private readonly ILeaderRepository _leaderRepository;
        private readonly IUserRepository _userRepository;

        public LeaderController(ILeaderRepository leaderRepository,
            IUserRepository userRepository)
        {
            _leaderRepository = leaderRepository;
            _userRepository = userRepository;
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
