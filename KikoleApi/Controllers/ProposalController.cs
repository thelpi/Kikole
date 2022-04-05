using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class ProposalController : KikoleBaseController
    {
        private readonly IPlayerService _playerService;
        private readonly IProposalRepository _proposalRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IBadgeService _badgeService;
        private readonly IClock _clock;
        private readonly TextResources _resources;

        public ProposalController(IProposalRepository proposalRepository,
            ILeaderRepository leaderRepository,
            IBadgeService badgeService,
            IPlayerService playerService,
            TextResources resources,
            IClock clock)
        {
            _proposalRepository = proposalRepository;
            _leaderRepository = leaderRepository;
            _badgeService = badgeService;
            _playerService = playerService;
            _resources = resources;
            _clock = clock;
        }

        [HttpGet("proposal-charts")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(ProposalChart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ProposalChart>> GetProposalChartAsync()
        {
            ProposalChart.Default.FirstDate = await _playerService
                .GetFirstSubmittedPlayerDateAsync()
                .ConfigureAwait(false);
            return ProposalChart.Default;
        }

        [HttpPut("club-proposals")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitClubProposalAsync(
            [FromBody] ClubProposalRequest request,
            [FromQuery] ulong userId)
        {
            return await SubmitProposalAsync(request, userId).ConfigureAwait(false);
        }

        [HttpPut("year-proposals")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitYearProposalAsync(
            [FromBody] YearProposalRequest request,
            [FromQuery] ulong userId)
        {
            return await SubmitProposalAsync(request, userId).ConfigureAwait(false);
        }

        [HttpPut("name-proposals")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitNameProposalAsync(
            [FromBody] NameProposalRequest request,
            [FromQuery] ulong userId)
        {
            return await SubmitProposalAsync(request, userId).ConfigureAwait(false);
        }

        [HttpPut("country-proposals")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitCountryProposalAsync(
            [FromBody] CountryProposalRequest request,
            [FromQuery] ulong userId)
        {
            return await SubmitProposalAsync(request, userId).ConfigureAwait(false);
        }
        
        [HttpPut("position-proposals")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitPositionProposalAsync(
            [FromBody] PositionProposalRequest request,
            [FromQuery] ulong userId)
        {
            return await SubmitProposalAsync(request, userId).ConfigureAwait(false);
        }

        [HttpGet("proposals")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<ProposalResponse>>> GetProposalsAsync(
            [FromQuery] DateTime proposalDate,
            [FromQuery] ulong userId)
        {
            if (userId == 0)
                return BadRequest(_resources.InvalidUser);

            var datas = await _proposalRepository
                .GetProposalsAsync(proposalDate, userId)
                .ConfigureAwait(false);

            if (datas.Count == 0)
                return Ok(new List<ProposalResponse>(0));

            var pInfo = await _playerService
                .GetPlayerInfoAsync(proposalDate)
                .ConfigureAwait(false);

            return Ok(GetProposalResponsesWithPoints(datas, pInfo, out _));
        }

        private async Task<ActionResult<ProposalResponse>> SubmitProposalAsync<T>(T request,
            ulong userId)
            where T : BaseProposalRequest
        {
            if (request == null)
                return BadRequest(string.Format(_resources.InvalidRequest, "null"));

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest(string.Format(_resources.InvalidRequest, validityRequest));

            var firstDate = await _playerService
                .GetFirstSubmittedPlayerDateAsync()
                .ConfigureAwait(false);

            if (request.PlayerSubmissionDate.Date < firstDate.Date || request.PlayerSubmissionDate.Date > _clock.Now.Date)
                return BadRequest(_resources.InvalidDate);

            var pInfo = await _playerService
                .GetPlayerInfoAsync(request.PlayerSubmissionDate)
                .ConfigureAwait(false);

            var response = new ProposalResponse(request, pInfo, _resources);

            var proposalsAlready = await _proposalRepository
                .GetProposalsAsync(request.PlayerSubmissionDate, userId)
                .ConfigureAwait(false);

            var proposalMade = request.MatchAny(proposalsAlready);

            GetProposalResponsesWithPoints(proposalsAlready, pInfo, out int sourcePoints);

            response = response.WithTotalPoints(sourcePoints, proposalMade);

            if (!proposalMade)
            {
                await _proposalRepository
                    .CreateProposalAsync(request.ToDto(userId, response.Successful))
                    .ConfigureAwait(false);

                if (response.IsWin)
                {
                    var leader = new LeaderDto
                    {
                        Points = (ushort)response.TotalPoints,
                        ProposalDate = request.ProposalDate,
                        Time = Convert.ToUInt16(Math.Ceiling((_clock.Now - request.ProposalDate.Date).TotalMinutes)),
                        UserId = userId
                    };

                    var isToday = request.DaysBefore == 0;
                    if (isToday)
                    {
                        await _leaderRepository
                            .CreateLeaderAsync(leader)
                            .ConfigureAwait(false);
                    }

                    var leaderBadges = await _badgeService
                        .PrepareNewLeaderBadgesAsync(leader, pInfo.Player, proposalsAlready, isToday)
                        .ConfigureAwait(false);

                    foreach (var b in leaderBadges)
                        response.AddBadge(b);
                }
            }

            var proposalBadges = await _badgeService
                .PrepareNonLeaderBadgesAsync(userId, request)
                .ConfigureAwait(false);

            foreach (var b in proposalBadges)
                response.AddBadge(b);

            return Ok(response);
        }

        private List<ProposalResponse> GetProposalResponsesWithPoints(
            IEnumerable<ProposalDto> proposalDtos,
            PlayerFullDto player,
            out int points)
        {
            var totalPoints = ProposalChart.Default.BasePoints;
            int? indexOfEnd = null;
            int currentIndex = 0;
            var proposals = proposalDtos
                .OrderBy(pDto => pDto.CreationDate)
                .Select(pDto =>
                {
                    var pr = new ProposalResponse(pDto, player)
                        .WithTotalPoints(totalPoints, false);
                    totalPoints = pr.TotalPoints;
                    if (pr.IsWin && !indexOfEnd.HasValue)
                    {
                        indexOfEnd = currentIndex;
                    }
                    currentIndex++;
                    return pr;
                })
                .ToList();

            if (indexOfEnd.HasValue)
            {
                for (var i = proposals.Count - 1; i > indexOfEnd.Value; i--)
                    proposals.RemoveAt(i);
            }

            points = totalPoints;
            return proposals;
        }
    }
}
