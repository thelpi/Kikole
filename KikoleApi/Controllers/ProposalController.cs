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
    /// <summary>
    /// Proposals controller.
    /// </summary>
    /// <seealso cref="KikoleBaseController"/>
    public class ProposalController : KikoleBaseController
    {
        private readonly IProposalService _proposalService;
        private readonly IPlayerService _playerService;
        private readonly IClock _clock;
        private readonly IStringLocalizer<Translations> _resources;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="proposalService">Instance of <see cref="IProposalService"/>.</param>
        /// <param name="playerService">Instance of <see cref="IPlayerService"/>.</param>
        /// <param name="resources">Translation resources.</param>
        /// <param name="clock">Clock service.</param>
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

        /// <summary>
        /// Gets users with at least one proposal on the specified date.
        /// </summary>
        /// <param name="proposalDate">Proposal date.</param>
        /// <returns>Collection of users.</returns>
        [HttpGet("active-users")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<User>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<User>>> GetUsersWithProposalAsync([FromQuery] DateTime date)
        {
            var users = await _proposalService
                .GetUsersWithProposalAsync(date)
                .ConfigureAwait(false);

            return Ok(users);
        }

        /// <summary>
        /// Gets the score chart for proposals.
        /// </summary>
        /// <returns>The score chart.</returns>
        [HttpGet("proposal-charts")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(ProposalChart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ProposalChart>> GetProposalChartAsync()
        {
            ProposalChart.Default.FirstDate = await _playerService
                .GetFirstSubmittedPlayerDateAsync(false)
                .ConfigureAwait(false);
            return Ok(ProposalChart.Default);
        }

        /// <summary>
        /// Proposes a club.
        /// </summary>
        /// <param name="request">Proposal request.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Proposal response.</returns>
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

        /// <summary>
        /// Proposes a year.
        /// </summary>
        /// <param name="request">Proposal request.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Proposal response.</returns>
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

        /// <summary>
        /// Proposes a name.
        /// </summary>
        /// <param name="request">Proposal request.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Proposal response.</returns>
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

        /// <summary>
        /// Proposes a country.
        /// </summary>
        /// <param name="request">Proposal request.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Proposal response.</returns>
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

        /// <summary>
        /// Proposes a position.
        /// </summary>
        /// <param name="request">Proposal request.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Proposal response.</returns>
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

        /// <summary>
        /// Requests a clue.
        /// </summary>
        /// <param name="request">Proposal request.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Proposal response.</returns>
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

        /// <summary>
        /// Gets proposals for a specific date and user.
        /// </summary>
        /// <param name="proposalDate">Proposal date.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of proposals.</returns>
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
                .GetFirstSubmittedPlayerDateAsync(true)
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
