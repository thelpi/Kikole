﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Handlers;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;

namespace KikoleSite.Services
{
    /// <summary>
    /// Player service implementation.
    /// </summary>
    /// <seealso cref="IPlayerService"/>
    public class PlayerService : IPlayerService
    {
        private readonly IPlayerHandler _playerHandler;
        private readonly IPlayerRepository _playerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IClock _clock;
        private readonly Random _randomizer;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="playerHandler">Instance of <see cref="IPlayerHandler"/>.</param>
        /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
        /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
        /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
        /// <param name="clock">Clock service.</param>
        /// <param name="randomizer">Randomizer.</param>
        public PlayerService(IPlayerHandler playerHandler,
            IPlayerRepository playerRepository,
            IUserRepository userRepository,
            ILeaderRepository leaderRepository,
            IClock clock,
            Random randomizer)
        {
            _playerHandler = playerHandler;
            _playerRepository = playerRepository;
            _userRepository = userRepository;
            _leaderRepository = leaderRepository;
            _clock = clock;
            _randomizer = randomizer;
        }

        /// <inheritdoc />
        public async Task<PlayerFullDto> GetPlayerOfTheDayFullInfoAsync(DateTime date)
        {
            return await _playerHandler
                .GetPlayerOfTheDayFullInfoAsync(date)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdatePlayerCluesAsync(ulong playerId,
            string clue,
            string easyClue,
            IReadOnlyDictionary<Languages, string> clueLanguages,
            IReadOnlyDictionary<Languages, string> easyClueLanguages)
        {
            await UpdateCluesInternalAsync(
                    playerId, clue, easyClue, clueLanguages, easyClueLanguages)
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
        public async Task<string> GetPlayerClueAsync(DateTime proposalDate, bool isEasy, Languages language)
        {
            var player = await _playerRepository
                .GetPlayerOfTheDayAsync(proposalDate)
                .ConfigureAwait(false);

            var clue = isEasy
                ? player.EasyClue
                : player.Clue;

            if (language != Languages.en)
            {
                clue = await _playerRepository
                    .GetClueAsync(player.Id, (byte)(isEasy ? 1 : 0), (ulong)language)
                    .ConfigureAwait(false);
            }

            return clue;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<Languages, (string clue, string easyclue)>> GetPlayerCluesAsync(ulong playerId, IReadOnlyCollection<Languages> languages)
        {
            var clues = new Dictionary<Languages, (string clue, string easyclue)>();

            if (languages.Contains(Languages.en))
            {
                var player = await _playerRepository
                    .GetPlayerByIdAsync(playerId)
                    .ConfigureAwait(false);

                clues.Add(Languages.en, (player.Clue, player.EasyClue));
            }

            foreach (var language in languages.Where(_ => _ != Languages.en))
            {
                var clue = await _playerRepository
                    .GetClueAsync(playerId, 0, (ulong)language)
                    .ConfigureAwait(false);

                var easyClue = await _playerRepository
                    .GetClueAsync(playerId, 1, (ulong)language)
                    .ConfigureAwait(false);

                clues.Add(language, (clue, easyClue));
            }

            return clues;
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

            await UpdateCluesInternalAsync(
                    request.PlayerId, clueEn, easyClueEn, request.ClueEditLanguages, request.EasyClueEditLanguages)
                .ConfigureAwait(false);

            await _playerRepository
                .ValidatePlayerProposalAsync(request.PlayerId, latestDate)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<PlayerCreator> GetPlayerOfTheDayFromUserPovAsync(
            ulong userId,
            DateTime proposalDate)
        {
            var player = await _playerRepository
                .GetPlayerOfTheDayAsync(proposalDate.Date)
                .ConfigureAwait(false);

            var creatorUser = await _userRepository
                .GetUserByIdAsync(player.CreationUserId)
                .ConfigureAwait(false);

            var requestUser = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            return new PlayerCreator(requestUser, player, creatorUser);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Player>> GetPlayerSubmissionsAsync()
        {
            var dtos = await _playerRepository
                .GetPendingValidationPlayersAsync()
                .ConfigureAwait(false);

            var users = new Dictionary<ulong, UserDto>();
            foreach (var usrId in dtos.Select(dto => dto.CreationUserId).Distinct())
            {
                var user = await _userRepository
                    .GetUserByIdAsync(usrId)
                    .ConfigureAwait(false);
                users.Add(usrId, user);
            }

            var players = new List<Player>(dtos.Count);
            foreach (var p in dtos)
            {
                var pInfo = await _playerHandler
                    .GetPlayerFullInfoAsync(p)
                    .ConfigureAwait(false);

                players.Add(new Player(pInfo, users.Values));
            }

            return players;
        }

        /// <inheritdoc />
        public async Task<(PlayerSubmissionErrors, ulong, IReadOnlyCollection<Badges>)> ValidatePlayerSubmissionAsync(PlayerSubmissionValidationRequest request)
        {
            var badges = new List<Badges>();

            var p = await _playerRepository
                .GetPlayerByIdAsync(request.PlayerId)
                .ConfigureAwait(false);

            if (p == null)
                return (PlayerSubmissionErrors.PlayerNotFound, 0, badges);

            if (p.ProposalDate.HasValue || p.RejectDate.HasValue)
                return (PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused, 0, badges);

            if (request.IsAccepted)
            {
                await AcceptSubmittedPlayerAsync(
                        request, p.Clue, p.EasyClue)
                    .ConfigureAwait(false);

                badges.Add(Badges.DoItYourself);

                var players = await _playerRepository
                    .GetPlayersByCreatorAsync(p.CreationUserId, true)
                    .ConfigureAwait(false);

                if (players.Count == 5)
                    badges.Add(Badges.WeAreKikole);

                // TODO: notify (+ badge)
            }
            else
            {
                await _playerRepository
                    .RefusePlayerProposalAsync(request.PlayerId)
                    .ConfigureAwait(false);

                // TODO: notify refusal
            }

            return (PlayerSubmissionErrors.NoError, p.CreationUserId, badges);
        }

        /// <inheritdoc />
        public async Task ReassignPlayersOfTheDayAsync()
        {
            if (_clock.IsTomorrowIn(30))
                return;

            var randomizedPlayers = (await _playerRepository
                .GetPlayersOfTheDayAsync(_clock.Tomorrow, null)
                .ConfigureAwait(false))
                .OrderBy(_ => _randomizer.Next())
                .ToList();

            var i = 0;
            foreach (var p in randomizedPlayers)
            {
                await _playerRepository
                    .ChangePlayerProposalDateAsync(p.Id, _clock.Tomorrow.AddDays(i))
                    .ConfigureAwait(false);
                i++;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CanDisplayHiddenPlayerAsync(ulong userId)
        {
            var leaderFound = await _leaderRepository
                .GetUserLeadersAsync(ProposalChart.HiddenDate, ProposalChart.HiddenDate, false, userId)
                .ConfigureAwait(false);

            if (leaderFound.Count > 0)
            {
                return true;
            }

            var createdPlayers = await _playerRepository
                .GetPlayersByCreatorAsync(userId, true)
                .ConfigureAwait(false);

            var leaders = await _leaderRepository
                .GetUserLeadersAsync(ProposalChart.FirstDate, null, false, userId)
                .ConfigureAwait(false);

            var countToFind = (_clock.Today - ProposalChart.FirstDate).Days + 1;

            var createdCount = createdPlayers.Count(_ => _.ProposalDate <= _clock.Today);

            return leaders.Count + createdCount == countToFind;
        }

        private async Task<DateTime> GetNextDateAsync()
        {
            var latestDate = await _playerRepository
                .GetLatestProposalDateAsync()
                .ConfigureAwait(false);

            return latestDate.AddDays(1).Date;
        }

        private async Task InsertLanguageCluesAsync(IReadOnlyDictionary<Languages, string> clues,
            ulong playerId, bool isEasy)
        {
            var languagesClues = clues?
                .Where(_ => !string.IsNullOrWhiteSpace(_.Value))
                .ToDictionary(_ => (ulong)_.Key, _ => _.Value.Trim())
                ?? new Dictionary<ulong, string>();

            if (languagesClues.Count > 0)
            {
                await _playerRepository
                    .InsertPlayerCluesByLanguageAsync(playerId, (byte)(isEasy ? 1 : 0), languagesClues)
                    .ConfigureAwait(false);
            }
        }

        private async Task UpdateCluesInternalAsync(ulong playerId,
            string clueEn,
            string easyClueEn,
            IReadOnlyDictionary<Languages, string> clueLanguages,
            IReadOnlyDictionary<Languages, string> easyClueLanguages)
        {
            await _playerRepository
                .UpdatePlayerCluesAsync(playerId, clueEn, easyClueEn)
                .ConfigureAwait(false);

            await InsertLanguageCluesAsync(
                    clueLanguages, playerId, false)
                .ConfigureAwait(false);

            await InsertLanguageCluesAsync(
                    easyClueLanguages, playerId, true)
                .ConfigureAwait(false);
        }
    }
}
