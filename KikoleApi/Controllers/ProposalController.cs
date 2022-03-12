using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class ProposalController : KikoleBaseController
    {
        private readonly IProposalRepository _proposalRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IClubRepository _clubRepository;
        private readonly IClock _clock;

        public ProposalController(IProposalRepository proposalRepository,
            IPlayerRepository playerRepository,
            IClubRepository clubRepository,
            ILeaderRepository leaderRepository,
            IClock clock)
        {
            _proposalRepository = proposalRepository;
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
            _leaderRepository = leaderRepository;
            _clock = clock;
        }

        [HttpGet("proposal-charts")]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType(typeof(ProposalChart), (int)HttpStatusCode.OK)]
        public ActionResult<ProposalChart> GetProposalChart()
        {
            return ProposalChart.Default;
        }

        [HttpPut("club-proposals")]
        [AuthenticationLevel(AuthenticationLevel.AuthenticatedOrAnonymous)]
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
        [AuthenticationLevel(AuthenticationLevel.AuthenticatedOrAnonymous)]
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
        [AuthenticationLevel(AuthenticationLevel.AuthenticatedOrAnonymous)]
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
        [AuthenticationLevel(AuthenticationLevel.AuthenticatedOrAnonymous)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitCountryProposalAsync(
            [FromBody] CountryProposalRequest request,
            [FromQuery] ulong userId)
        {
            return await SubmitProposalAsync(request, userId).ConfigureAwait(false);
        }

        [HttpPut("clue-proposals")]
        [AuthenticationLevel(AuthenticationLevel.AuthenticatedOrAnonymous)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitClueProposalAsync(
            [FromBody] ClueProposalRequest request,
            [FromQuery] ulong userId)
        {
            return await SubmitProposalAsync(request, userId).ConfigureAwait(false);
        }

        [HttpPut("position-proposals")]
        [AuthenticationLevel(AuthenticationLevel.AuthenticatedOrAnonymous)]
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
        [AuthenticationLevel(AuthenticationLevel.AuthenticatedOrAnonymous)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<ProposalResponse>>> GetProposalsAsync(
            [FromQuery] DateTime proposalDate,
            [FromQuery] ulong userId,
            [FromQuery] string userIp)
        {
            if (userId == 0 && string.IsNullOrWhiteSpace(userIp))
                return BadRequest("IP is required");

            var datas = await _proposalRepository
                .GetProposalsAsync(proposalDate, userId, userIp)
                .ConfigureAwait(false);

            if (datas.Count == 0)
                return Ok(new List<ProposalResponse>(0));

            var playerOfTheDay = await _playerRepository
                .GetPlayerOfTheDayAsync(proposalDate)
                .ConfigureAwait(false);

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

            int totalPoints = ProposalChart.Default.BasePoints;
            int? indexOfEnd = null;
            int currentIndex = 0;
            var proposals = datas
                .OrderBy(pDto => pDto.CreationDate)
                .Select(pDto =>
                {
                    var pr = new ProposalResponse(
                            pDto,
                            playerOfTheDay,
                            playerClubs,
                            playerClubsDetails)
                        .WithTotalPoints(totalPoints);
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

            return Ok(proposals);
        }

        private async Task<ActionResult<ProposalResponse>> SubmitProposalAsync<T>(T request,
            ulong userId)
            where T : BaseProposalRequest
        {
            if (request == null)
                return BadRequest("Invalid request: null");

            var validityRequest = request.IsValid();
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest($"Invalid request: {validityRequest}");

            var playerOfTheDay = await _playerRepository
                .GetPlayerOfTheDayAsync(request.PlayerSubmissionDate)
                .ConfigureAwait(false);

            if (playerOfTheDay == null)
                return BadRequest("Invalid proposal date");

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

            var response = new ProposalResponse(
                    request, 
                    playerOfTheDay,
                    playerClubs,
                    playerClubsDetails)
                .WithTotalPoints(request.SourcePoints);
            
            await _proposalRepository
                .CreateProposalAsync(request.ToDto(userId, response.Successful))
                .ConfigureAwait(false);

            if (response.IsWin && request.DaysBefore == 0)
            {
                await _leaderRepository
                    .CreateLeaderAsync(new LeaderDto
                    {
                        Ip = request.UserIp,
                        Points = (ushort)response.TotalPoints,
                        ProposalDate = request.ProposalDate,
                        Time = Convert.ToUInt16(Math.Ceiling((_clock.Now - request.ProposalDate.Date).TotalMinutes)),
                        UserId = userId
                    })
                    .ConfigureAwait(false);
            }

            return Ok(response);
        }
    }
}
