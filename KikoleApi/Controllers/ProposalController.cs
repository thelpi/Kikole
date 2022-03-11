using System;
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
    public class ProposalController : KikoleBaseController
    {
        private readonly IProposalRepository _proposalRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IClubRepository _clubRepository;

        public ProposalController(IProposalRepository proposalRepository,
            IPlayerRepository playerRepository,
            IClubRepository clubRepository)
        {
            _proposalRepository = proposalRepository;
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
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
        [AuthenticationLevel(AuthenticationLevel.Authenticated)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<IReadOnlyCollection<ProposalResponse>>> GetProposalsAsync(
            [FromQuery] DateTime proposalDate,
            [FromQuery] ulong userId)
        {
            var datas = await _proposalRepository
                .GetProposalsAsync(proposalDate, userId)
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

            return Ok(datas.Select(d => new ProposalResponse(d, playerOfTheDay, playerClubs, playerClubsDetails)).ToList());
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

            // TODO: get points from userId
            // and override value from request

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

            var response = request.CheckSuccessful(
                playerOfTheDay,
                playerClubs,
                playerClubsDetails);
            
            if (userId > 0)
            {
                await _proposalRepository
                    .CreateProposalAsync(request.ToDto(userId, response.Successful))
                    .ConfigureAwait(false);
            }

            return Ok(response);
        }
    }
}
