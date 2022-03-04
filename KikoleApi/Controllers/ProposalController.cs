using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    [Route("proposals")]
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

        [HttpPost]
        [ProducesResponseType(typeof(ProposalResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ProposalResponse>> SubmitProposalAsync([FromBody] ProposalRequest request)
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

            var successful = false;
            var value = request.Value;
            switch (request.ProposalType)
            {
                case ProposalType.Club:
                    foreach (var pc in playerClubs)
                    {
                        var c = await _clubRepository
                            .GetClubAsync(pc.ClubId)
                            .ConfigureAwait(false);
                        if (c.AllowedNames.Contains(value.Sanitize()))
                        {
                            successful = true;
                            break;
                        }
                    }
                    break;
                case ProposalType.Clue:
                    successful = true;
                    var clubId = playerClubs.OrderBy(pc => pc.ImportancePosition).First().ClubId;
                    var club = await _clubRepository
                        .GetClubAsync(clubId)
                        .ConfigureAwait(false);
                    value = club.Name;
                    break;
                case ProposalType.Country:
                    var countryId = (ulong)Enum.Parse<Country>(value);
                    successful = todayPlayer.Country1Id == countryId
                        || todayPlayer.Country2Id == countryId;
                    break;
                case ProposalType.Name:
                    successful = todayPlayer.AllowedNames.Contains(value.Sanitize());
                    break;
                case ProposalType.Year:
                    successful = int.Parse(value) == todayPlayer.YearOfBirth;
                    break;
            }

            var userid = GetAuthenticatedPlayer();
            if (userid.HasValue)
            {
                await _proposalRepository
                    .CreateProposalAsync(request.ToDto(userid.Value, successful))
                    .ConfigureAwait(false);
            }

            return Ok(new ProposalResponse
            {
                Successful = successful,
                Value = value
            });
        }
    }
}
