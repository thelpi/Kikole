using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces.Handlers;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Handlers
{
    /// <summary>
    /// Player handler implementation.
    /// </summary>
    /// <seealso cref="IPlayerHandler"/>
    public class PlayerHandler : IPlayerHandler
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IClubRepository _clubRepository;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
        /// <param name="clubRepository">Instance of <see cref="IClubRepository"/>.</param>
        public PlayerHandler(IPlayerRepository playerRepository,
            IClubRepository clubRepository)
        {
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
        }

        /// <inheritdoc />
        public async Task<PlayerFullDto> GetPlayerOfTheDayFullInfoAsync(DateTime date)
        {
            var p = await _playerRepository
                .GetPlayerOfTheDayAsync(date)
                .ConfigureAwait(false);

            return await GetPlayerFullInfoAsync(p)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<PlayerFullDto> GetPlayerFullInfoAsync(PlayerDto p)
        {
            var playerClubs = await _playerRepository
                .GetPlayerClubsAsync(p.Id)
                .ConfigureAwait(false);

            var playerClubsDetails = new Dictionary<ulong, ClubDto>(playerClubs.Count);
            foreach (var pc in playerClubs)
            {
                if (!playerClubsDetails.ContainsKey(pc.ClubId))
                {
                    var c = await _clubRepository
                        .GetClubAsync(pc.ClubId)
                        .ConfigureAwait(false);
                    playerClubsDetails.Add(pc.ClubId, c);
                }
            }

            return new PlayerFullDto
            {
                Clubs = playerClubsDetails.Values.ToList(),
                Player = p,
                PlayerClubs = playerClubs
            };
        }
    }
}
