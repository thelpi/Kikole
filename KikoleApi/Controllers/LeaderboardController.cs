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
    public class LeaderboardController : KikoleBaseController
    {
        private readonly ILeaderboardRepository _leaderboardRepository;
        private readonly IUserRepository _userRepository;

        public LeaderboardController(ILeaderboardRepository leaderboardRepository,
            IUserRepository userRepository)
        {
            _leaderboardRepository = leaderboardRepository;
            _userRepository = userRepository;
        }

        [HttpGet("leaderboards")]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType(typeof(IReadOnlyCollection<Leaderboard>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Leaderboard>>> GetLeaderboardsAsync(
            [FromQuery] DateTime? minimalDate,
            [FromQuery] bool includeAnonymous,
            [FromQuery] LeaderboardSort leaderboardSort)
        {
            var dtos = await _leaderboardRepository
                .GetLeaderboardsAsync(minimalDate, includeAnonymous)
                .ConfigureAwait(false);

            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            var leaders = dtos
                .GroupBy(dto => dto.Key)
                .Select(dto => new Leaderboard(dto, users))
                .ToList();

            switch (leaderboardSort)
            {
                case LeaderboardSort.SuccessCount:
                    leaders = leaders.OrderByDescending(l => l.SuccessCount).ToList();
                    break;
                case LeaderboardSort.BestTime:
                    leaders = leaders.OrderBy(l => l.BestTime.TotalMinutes).ToList();
                    break;
                case LeaderboardSort.TotalPoints:
                    leaders = leaders.OrderByDescending(l => l.TotalPoints).ToList();
                    break;
            }

            return Ok(leaders);
        }
    }
}
