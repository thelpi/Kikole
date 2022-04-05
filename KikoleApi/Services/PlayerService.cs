using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IClubRepository _clubRepository;

        public PlayerService(IPlayerRepository playerRepository,
            IClubRepository clubRepository)
        {
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
        }

        /// <inheritdoc />
        public async Task<PlayerFullDto> GetPlayerInfoAsync(DateTime date)
        {
            var playerOfTheDay = await _playerRepository
                .GetPlayerOfTheDayAsync(date)
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

            return new PlayerFullDto
            {
                Clubs = playerClubsDetails,
                Player = playerOfTheDay,
                PlayerClubs = playerClubs
            };
        }

        public async Task<DateTime> GetFirstSubmittedPlayerDateAsync()
        {
            return await _playerRepository
                .GetFirstDateAsync()
                .ConfigureAwait(false);
        }
    }
}
