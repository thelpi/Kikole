using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;

namespace KikoleApi.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IClubRepository _clubRepository;
        private readonly TextResources _resources;

        public PlayerService(IPlayerRepository playerRepository,
            IClubRepository clubRepository,
            TextResources resources)
        {
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
            _resources = resources;
        }

        /// <inheritdoc />
        public async Task<PlayerFullDto> GetPlayerInfoAsync(DateTime date)
        {
            var playerOfTheDay = await _playerRepository
                .GetPlayerOfTheDayAsync(date)
                .ConfigureAwait(false);

            return await GetPlayerInfoAsync(playerOfTheDay).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<PlayerFullDto> GetPlayerInfoAsync(PlayerDto p)
        {
            var playerClubs = await _playerRepository
                .GetPlayerClubsAsync(p.Id)
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
                Player = p,
                PlayerClubs = playerClubs
            };
        }

        /// <inheritdoc />
        public async Task<DateTime> GetFirstSubmittedPlayerDateAsync()
        {
            return await _playerRepository
                .GetFirstDateAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ulong> CreatePlayerAsync(PlayerRequest request, ulong userId)
        {
            if (!request.ProposalDate.HasValue && request.SetLatestProposalDate)
                request.ProposalDate = await GetNextDateAsync().ConfigureAwait(false);

            var playerId = await _playerRepository
                .CreatePlayerAsync(request.ToDto(userId))
                .ConfigureAwait(false);

            var languagesClues = new Dictionary<ulong, string>();
            if (request.ClueLanguages != null)
            {
                foreach (var kvp in request.ClueLanguages)
                {
                    var actualValue = kvp.Value?.Trim();
                    if (!string.IsNullOrWhiteSpace(actualValue))
                        languagesClues.Add((ulong)kvp.Key, actualValue);
                }
            }

            if (languagesClues.Count > 0)
            {
                await _playerRepository
                    .InsertPlayerCluesByLanguageAsync(playerId, languagesClues)
                    .ConfigureAwait(false);
            }

            foreach (var club in request.ToPlayerClubDtos(playerId))
            {
                await _playerRepository
                    .CreatePlayerClubsAsync(club)
                    .ConfigureAwait(false);
            }

            return playerId;
        }

        /// <inheritdoc />
        public async Task<string> GetPlayerClueAsync(DateTime proposalDate)
        {
            var player = await _playerRepository
                .GetPlayerOfTheDayAsync(proposalDate)
                .ConfigureAwait(false);

            var clue = player.Clue;
            if (_resources.Language != Languages.en)
            {
                clue = await _playerRepository
                    .GetClueAsync(player.Id, (ulong)_resources.Language)
                    .ConfigureAwait(false);
            }

            return clue;
        }

        /// <inheritdoc />
        public async Task AcceptSubmittedPlayerAsync(PlayerSubmissionValidationRequest request, string currentClue)
        {
            var clueEn = string.IsNullOrWhiteSpace(request.ClueEditEn)
                ? currentClue
                : request.ClueEditEn.Trim();

            var latestDate = await GetNextDateAsync().ConfigureAwait(false);

            await _playerRepository
                .ValidatePlayerProposalAsync(request.PlayerId, clueEn, latestDate)
                .ConfigureAwait(false);

            var languagesClues = new Dictionary<ulong, string>();
            foreach (var kvp in request.ClueEditLangugages)
                languagesClues.Add((ulong)kvp.Key, kvp.Value.Trim());

            await _playerRepository
                .InsertPlayerCluesByLanguageAsync(request.PlayerId, languagesClues)
                .ConfigureAwait(false);
        }

        private async Task<DateTime> GetNextDateAsync()
        {
            var latestDate = await _playerRepository
                .GetLatestProposalDateAsync()
                .ConfigureAwait(false);

            return latestDate.AddDays(1).Date;
        }
    }
}
