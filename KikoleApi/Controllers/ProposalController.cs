using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using Microsoft.AspNetCore.Http;
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
            IClubRepository clubRepository,
            IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _proposalRepository = proposalRepository;
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
        }

        [HttpPut("club-proposals")]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitClubProposalAsync([FromBody] ClubProposalRequest request)
        {
            return await SubmitProposalAsync(request).ConfigureAwait(false);
        }

        [HttpPut("year-proposals")]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitYearProposalAsync([FromBody] YearProposalRequest request)
        {
            return await SubmitProposalAsync(request).ConfigureAwait(false);
        }

        [HttpPut("name-proposals")]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitNameProposalAsync([FromBody] NameProposalRequest request)
        {
            return await SubmitProposalAsync(request).ConfigureAwait(false);
        }

        [HttpPut("country-proposals")]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitCountryProposalAsync([FromBody] CountryProposalRequest request)
        {
            return await SubmitProposalAsync(request).ConfigureAwait(false);
        }

        [HttpPut("clue-proposals")]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitClueProposalAsync([FromBody] ClueProposalRequest request)
        {
            return await SubmitProposalAsync(request).ConfigureAwait(false);
        }

        private async Task<ActionResult<ProposalResponse>> SubmitProposalAsync<T>(T request)
            where T : BaseProposalRequest
        {
            if (IsFaultedAuthentication())
                return Unauthorized();

            if (request == null)
                return BadRequest("Invalid request: null");

            var validityRequest = request.IsValid();
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest($"Invalid request: {validityRequest}");

            var todayPlayer = await _playerRepository
                .GetTodayPlayerAsync(request.ProposalDate)
                .ConfigureAwait(false);

            if (todayPlayer == null)
                return BadRequest("Invalid proposal date");

            var playerClubs = await _playerRepository
                .GetPlayerClubsAsync(todayPlayer.Id)
                .ConfigureAwait(false);

            var playerClubsDetails = new List<ClubDto>(playerClubs.Count);
            foreach (var pc in playerClubs)
            {
                var c = await _clubRepository
                    .GetClubAsync(pc.ClubId)
                    .ConfigureAwait(false);
                playerClubsDetails.Add(c);
            }

            var successfulValue = request.IsSuccessful(
                todayPlayer,
                playerClubs,
                playerClubsDetails);

            var successful = !string.IsNullOrWhiteSpace(successfulValue);

            var userId = GetAuthenticatedUser();
            if (userId.HasValue)
            {
                await _proposalRepository
                    .CreateProposalAsync(request.ToDto(userId.Value, successful))
                    .ConfigureAwait(false);
            }

            return Ok(new ProposalResponse
            {
                Successful = successful,
                Value = successfulValue
            });
        }
    }
}
