using System.Threading.Tasks;
using KikoleApi.Domain.Models;
using KikoleApi.Domain.Models.Dtos;
using KikoleApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    [Route("players")]
    public class PlayerController : ControllerBase
    {
        private readonly PlayerRepository _playerRepository;

        public PlayerController(PlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        [HttpPost]
        [ProducesResponseType(201)]
        public async Task<IActionResult> CreatePlayerAsync([FromBody] PlayerRequest request)
        {
            var pDto = new PlayerDto
            {
                Country1Id = (ulong)request.Country,
                Country2Id = (ulong?)request.SecondCountry,
                Name = request.Name,
                ProposalDate = request.ProposalDate,
                YearOfBirth = (uint)request.DateOfBirth.Year
            };

            var playerId = await _playerRepository
                .CreatePlayerAsync(pDto)
                .ConfigureAwait(false);

            foreach (var name in  request.AllowedNames)
            {
                var pnDto = new PlayerNameDto
                {
                    Name = name,
                    PlayerId = playerId
                };

                await _playerRepository
                    .CreatePlayerNamesAsync(pnDto)
                    .ConfigureAwait(false);
            }

            foreach (var club in request.Clubs)
            {
                var pcDto = new PlayerClubDto
                {
                    AllowedNames = string.Join(";", club.AllowedNames),
                    HistoryPosition = club.HistoryPosition,
                    ImportancePosition = club.ImportancePosition,
                    Name = club.Name,
                    PlayerId = playerId
                };

                await _playerRepository
                    .CreatePlayerClubsAsync(pcDto)
                    .ConfigureAwait(false);
            }

            return StatusCode(201);
        }
    }
}
