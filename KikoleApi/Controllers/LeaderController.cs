using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces.Services;
using KikoleApi.Models;
using KikoleApi.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleApi.Controllers
{
    public class LeaderController : KikoleBaseController
    {
        private readonly ILeaderService _leaderService;
        private readonly IBadgeService _badgeService;
        private readonly IStringLocalizer<Translations> _resources;

        public LeaderController(ILeaderService leaderService,
            IBadgeService badgeService,
            IStringLocalizer<Translations> resources)
        {
            _resources = resources;
            _leaderService = leaderService;
            _badgeService = badgeService;
        }

        [HttpPut("/recompute-badges")]
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
            [FromQuery] DateTime day,
            [FromQuery] LeaderSorts sort)
        {
            if (sort == LeaderSorts.SuccessCount)
                return BadRequest(_resources["SuccessCountSortForbidden"]);

            var dayLeaders = await _leaderService
                .GetLeadersOfTheDayAsync(day, sort)
                .ConfigureAwait(false);

            return Ok(dayLeaders);
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
                return BadRequest(_resources["InvalidSortType"]);

            if (minimalDate.HasValue && maximalDate.HasValue && minimalDate.Value.Date > maximalDate.Value.Date)
                return BadRequest(_resources["InvalidDateRange"]);

            IReadOnlyCollection<Leader> leaders;
            if (includePvp)
            {
                leaders = await _leaderService
                    .GetPvpLeadersAsync(minimalDate, maximalDate)
                    .ConfigureAwait(false);
            }
            else
            {
                leaders = await _leaderService
                    .GetPveLeadersAsync(minimalDate, maximalDate, leaderSort)
                    .ConfigureAwait(false);
            }

            return Ok(leaders);
        }

        [HttpGet("/awards")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(Awards), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<Awards>> GetAwardsOfTheMonthAsync(
            [FromQuery] DateTime date)
        {
            var awards = await _leaderService
                .GetAwardsAsync(date.Year, date.Month)
                .ConfigureAwait(false);

            return Ok(awards);
        }

        [HttpGet("/users/{userId}/stats")]
        [ProducesResponseType(typeof(UserStat), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<UserStat>> GetUserStatsAsync(
            [FromRoute] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources["InvalidUser"]);

            var userStatistics = await _leaderService
                .GetUserStatisticsAsync(userId)
                .ConfigureAwait(false);

            if (userStatistics == null)
                return NotFound(_resources["UserDoesNotExist"]);

            return Ok(userStatistics);
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
            [FromRoute] ulong id,
            [FromQuery] ulong userId)
        {
            if (id == 0)
                return BadRequest(_resources["InvalidUser"]);

            var badgesFull = await _badgeService
                 .GetUserBadgesAsync(id, userId)
                 .ConfigureAwait(false);

            return Ok(badgesFull);
        }
    }
}
