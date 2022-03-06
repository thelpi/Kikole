﻿using System.Net;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Controllers
{
    [Route("players")]
    public class PlayerController : KikoleBaseController
    {
        private readonly IPlayerRepository _playerRepository;

        public PlayerController(IPlayerRepository playerRepository,
            IHttpContextAccessor httpContextAccessor,
            ICrypter crypter,
            IConfiguration configuration)
            : base(httpContextAccessor, crypter, configuration)
        {
            _playerRepository = playerRepository;
        }

        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreatePlayerAsync([FromBody] PlayerRequest request)
        {
            if (!IsAdminAuthentification())
                return Unauthorized();

            if (request == null)
                return BadRequest("Invalid request: null");

            var validityRequest = request.IsValid();
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return BadRequest($"Invalid request: {validityRequest}");

            var playerId = await _playerRepository
                .CreatePlayerAsync(request.ToDto())
                .ConfigureAwait(false);

            if (playerId == 0)
                return StatusCode((int)HttpStatusCode.InternalServerError, "Player creation failure");
            
            foreach (var club in request.ToPlayerClubDtos(playerId))
            {
                await _playerRepository
                    .CreatePlayerClubsAsync(club)
                    .ConfigureAwait(false);
            }

            return Created($"players/{playerId}", null);
        }
    }
}
