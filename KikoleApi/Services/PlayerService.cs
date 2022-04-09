using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Http;

namespace KikoleApi.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IClubRepository _clubRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IClock _clock;

        public PlayerService(IPlayerRepository playerRepository,
            IClubRepository clubRepository,
            IHttpContextAccessor httpContextAccessor,
            IClock clock)
        {
            _playerRepository = playerRepository;
            _clubRepository = clubRepository;
            _httpContextAccessor = httpContextAccessor;
            _clock = clock;
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

            await InsertLanguageCluesAsync(
                    request.ClueLanguages, playerId, false)
                .ConfigureAwait(false);

            await InsertLanguageCluesAsync(
                    request.EasyClueLanguages, playerId, true)
                .ConfigureAwait(false);

            foreach (var club in request.ToPlayerClubDtos(playerId))
            {
                await _playerRepository
                    .CreatePlayerClubsAsync(club)
                    .ConfigureAwait(false);
            }

            return playerId;
        }

        /// <inheritdoc />
        public async Task<string> GetPlayerClueAsync(DateTime proposalDate, bool isEasy)
        {
            var player = await _playerRepository
                .GetPlayerOfTheDayAsync(proposalDate)
                .ConfigureAwait(false);

            var clue = isEasy
                ? player.EasyClue
                : player.Clue;

            var lng = _httpContextAccessor.ExtractLanguage();
            if (lng != Languages.en)
            {
                clue = await _playerRepository
                    .GetClueAsync(player.Id, (byte)(isEasy ? 1 : 0), (ulong)lng)
                    .ConfigureAwait(false);
            }

            return clue;
        }

        /// <inheritdoc />
        public async Task AcceptSubmittedPlayerAsync(PlayerSubmissionValidationRequest request,
            string currentClue, string currentEasyClue)
        {
            var clueEn = string.IsNullOrWhiteSpace(request.ClueEditEn)
                ? currentClue
                : request.ClueEditEn.Trim();

            var easyClueEn = string.IsNullOrWhiteSpace(request.EasyClueEditEn)
                ? currentEasyClue
                : request.EasyClueEditEn.Trim();

            var latestDate = await GetNextDateAsync().ConfigureAwait(false);

            await _playerRepository
                .ValidatePlayerProposalAsync(request.PlayerId, clueEn, easyClueEn, latestDate)
                .ConfigureAwait(false);

            await InsertLanguageCluesAsync(
                    request.ClueEditLanguages, request.PlayerId, false)
                .ConfigureAwait(false);

            await InsertLanguageCluesAsync(
                    request.EasyClueEditLanguages, request.PlayerId, true)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<DateTime> ComputeAvailableChallengeDateAsync(
            ChallengeDto challenge,
            IReadOnlyCollection<DateTime> hostDates,
            IReadOnlyCollection<DateTime> guestDates)
        {
            var challengeDate = _clock.Today;
            PlayerDto p;
            do
            {
                challengeDate = challengeDate.AddDays(1);

                p = await _playerRepository
                    .GetPlayerOfTheDayAsync(challengeDate)
                    .ConfigureAwait(false);
            }
            while (hostDates.Contains(challengeDate)
                || guestDates.Contains(challengeDate)
                || p.CreationUserId == challenge.GuestUserId
                || p.CreationUserId == challenge.HostUserId);

            return challengeDate;
        }

        private async Task<DateTime> GetNextDateAsync()
        {
            var latestDate = await _playerRepository
                .GetLatestProposalDateAsync()
                .ConfigureAwait(false);

            return latestDate.AddDays(1).Date;
        }

        private async Task InsertLanguageCluesAsync(IReadOnlyDictionary<string, string> clues,
            ulong playerId, bool isEasy)
        {
            var languagesClues = clues?
                .Where(_ => !string.IsNullOrWhiteSpace(_.Value)
                    && Enum.TryParse<Languages>(_.Key, out var lng))
                .ToDictionary(_ => (ulong)Enum.Parse<Languages>(_.Key), _ => _.Value.Trim())
                ?? new Dictionary<ulong, string>();

            if (languagesClues.Count > 0)
            {
                await _playerRepository
                    .InsertPlayerCluesByLanguageAsync(playerId, (byte)(isEasy ? 1 : 0), languagesClues)
                    .ConfigureAwait(false);
            }
        }
    }
}
