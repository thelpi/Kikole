﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    [Route("clubs")]
    public class ClubController : KikoleBaseController
    {
        private readonly IClubRepository _clubRepository;

        public ClubController(IClubRepository clubRepository)
        {
            _clubRepository = clubRepository;
        }

        [HttpGet]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<Club>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<IReadOnlyCollection<Club>>> GetClubsAsync()
        {
            var clubs = await _clubRepository
                .GetClubsAsync()
                .ConfigureAwait(false);

            return Ok(clubs.Select(c => new Club(c)).OrderBy(c => c.Name).ToList());
        }

        [HttpPost]
        [AuthenticationLevel(UserTypes.PowerUser)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateClubAsync([FromBody] ClubRequest request)
        {
            if (request == null)
                return BadRequest(string.Format(SPA.TextResources.InvalidRequest, "null"));

            var validityRequest = request.IsValid();
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest(string.Format(SPA.TextResources.InvalidRequest, validityRequest));

            var playerId = await _clubRepository
                .CreateClubAsync(request.ToDto())
                .ConfigureAwait(false);

            if (playerId == 0)
                return StatusCode((int)HttpStatusCode.InternalServerError, SPA.TextResources.ClubCreationFailure);
            
            return Created($"clubs/{playerId}", null);
        }
    }
}
