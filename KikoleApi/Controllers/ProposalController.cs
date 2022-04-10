using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Interfaces.Services;
using KikoleApi.Models;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KikoleApi.Controllers
{
    public class ProposalController : KikoleBaseController
    {
        private readonly IProposalService _proposalService;
        private readonly IPlayerService _playerService;
        private readonly IClock _clock;
        private readonly IStringLocalizer<Translations> _resources;

        public ProposalController(IProposalService proposalService,
            IPlayerService playerService,
            IStringLocalizer<Translations> resources,
            IClock clock)
        {
            _proposalService = proposalService;
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

        [HttpPut("clue-proposals")]
        [AuthenticationLevel(UserTypes.StandardUser)]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<ProposalResponse>> SubmitClueProposalAsync(
            [FromBody] ClueProposalRequest request,
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
                return BadRequest(_resources["InvalidUser"]);

            var proposals = await _proposalService
                .GetProposalsAsync(proposalDate, userId)
                .ConfigureAwait(false);

            return Ok(proposals);
        }

        private async Task<ActionResult<ProposalResponse>> SubmitProposalAsync<T>(T request,
            ulong userId)
            where T : BaseProposalRequest
        {
            if (request == null)
                return BadRequest(string.Format(_resources["InvalidRequest"], "null"));

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest(string.Format(_resources["InvalidRequest"], validityRequest));

            var firstDate = await _playerService
                .GetFirstSubmittedPlayerDateAsync()
                .ConfigureAwait(false);

            if (request.PlayerSubmissionDate.Date < firstDate.Date || request.PlayerSubmissionDate.Date > _clock.Today)
                return BadRequest(_resources["InvalidDate"]);

            var pInfo = await _playerService
                .GetPlayerInfoAsync(request.PlayerSubmissionDate)
                .ConfigureAwait(false);

            var response = await _proposalService
                .ManageProposalResponseAsync(request, userId, pInfo)
                .ConfigureAwait(false);

            return Ok(response);
        }
    }
}
