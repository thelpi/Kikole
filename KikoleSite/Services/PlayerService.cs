using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Handlers;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;

namespace KikoleSite.Services;

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
    private readonly IGameCalendar _gameCalendar;
    private readonly Random _randomizer;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="playerHandler">Instance of <see cref="IPlayerHandler"/>.</param>
    /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
    /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
    /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
    /// <param name="clock">Clock service.</param>
    /// <param name="gameCalendar">Instance of <see cref="IGameCalendar"/>.</param>
    /// <param name="randomizer">Randomizer.</param>
    public PlayerService(IPlayerHandler playerHandler,
        IPlayerRepository playerRepository,
        IUserRepository userRepository,
        ILeaderRepository leaderRepository,
        IClock clock,
        IGameCalendar gameCalendar,
        Random randomizer)
    {
        _playerHandler = playerHandler;
        _playerRepository = playerRepository;
        _userRepository = userRepository;
        _leaderRepository = leaderRepository;
        _clock = clock;
        _gameCalendar = gameCalendar;
        _randomizer = randomizer;
    }

    /// <inheritdoc />
    public async Task<PlayerFullDto> GetPlayerOfTheDayFullInfoAsync(DateTime date)
    {
        return await _playerHandler
            .GetPlayerOfTheDayFullInfoAsync(date);
    }

    /// <inheritdoc />
    public async Task UpdatePlayerCluesAsync(ulong playerId,
        string clue,
        string easyClue,
        IReadOnlyDictionary<Languages, string?>? clueLanguages,
        IReadOnlyDictionary<Languages, string?>? easyClueLanguages)
    {
        await UpdateCluesInternalAsync(
                playerId, clue, easyClue, clueLanguages, easyClueLanguages);
    }

    /// <inheritdoc />
    public async Task<ulong> CreatePlayerAsync(PlayerRequest request, ulong userId)
    {
        // la date est resolue ici plutot qu'ecrite dans la requete : un service n'a pas
        // a modifier l'objet qu'on lui passe
        var publicationDate = request.PublicationDate;
        if (!publicationDate.HasValue && request.SetLatestPublicationDate)
            publicationDate = await GetNextDateAsync();

        var playerId = await _playerRepository
            .CreatePlayerAsync(request.ToDto(userId, publicationDate));

        await InsertLanguageCluesAsync(
                request.ClueLanguages, playerId, false);

        await InsertLanguageCluesAsync(
                request.EasyClueLanguages, playerId, true);

        foreach (var club in request.ToPlayerClubDtos(playerId))
        {
            await _playerRepository
                .CreatePlayerClubsAsync(club);
        }

        return playerId;
    }

    /// <inheritdoc />
    public async Task<string?> GetPlayerClueAsync(DateTime proposalDate, bool isEasy, Languages language)
    {
        var player = await _playerRepository
            .GetPlayerOfTheDayAsync(proposalDate)
            ?? throw new InvalidOperationException($"Aucun joueur n'est programme pour le {proposalDate:yyyy-MM-dd}.");

        var clue = isEasy
            ? player.EasyClue
            : player.Clue;

        if (language != Languages.en)
        {
            clue = await _playerRepository
                .GetClueAsync(player.Id, (byte)(isEasy ? 1 : 0), (ulong)language);
        }

        return clue;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Languages, (string? clue, string? easyclue)>> GetPlayerCluesAsync(ulong playerId, IReadOnlyCollection<Languages> languages)
    {
        var clues = new Dictionary<Languages, (string? clue, string? easyclue)>();

        if (languages.Contains(Languages.en))
        {
            var player = await _playerRepository
                .GetPlayerByIdAsync(playerId)
                ?? throw new InvalidOperationException($"Le joueur {playerId} est introuvable.");

            clues.Add(Languages.en, (player.Clue, player.EasyClue));
        }

        foreach (var language in languages.Where(_ => _ != Languages.en))
        {
            var clue = await _playerRepository
                .GetClueAsync(playerId, 0, (ulong)language);

            var easyClue = await _playerRepository
                .GetClueAsync(playerId, 1, (ulong)language);

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

        var latestDate = await GetNextDateAsync();

        await UpdateCluesInternalAsync(
                request.PlayerId, clueEn, easyClueEn, request.ClueEditLanguages, request.EasyClueEditLanguages);

        await _playerRepository
            .ValidatePlayerProposalAsync(request.PlayerId, latestDate);
    }

    /// <inheritdoc />
    public async Task<PlayerCreator> GetPlayerOfTheDayFromUserPovAsync(
        ulong userId,
        DateTime proposalDate)
    {
        var player = await _playerRepository
            .GetPlayerOfTheDayAsync(proposalDate.Date)
            ?? throw new InvalidOperationException($"Aucun joueur n'est programme pour le {proposalDate:yyyy-MM-dd}.");

        var creatorUser = await _userRepository
            .GetUserByIdAsync(player.CreationUserId)
            ?? throw new InvalidOperationException($"Le createur {player.CreationUserId} du joueur du jour est introuvable.");

        var requestUser = await _userRepository
            .GetUserByIdAsync(userId)
            ?? throw new InvalidOperationException($"L'utilisateur {userId} est introuvable.");

        return new PlayerCreator(requestUser, player, creatorUser);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Player>> GetPlayerSubmissionsAsync()
    {
        var dtos = await _playerRepository
            .GetPendingValidationPlayersAsync();

        var users = new Dictionary<ulong, UserDto>();
        foreach (var usrId in dtos.Select(dto => dto.CreationUserId).Distinct())
        {
            var user = await _userRepository
                .GetUserByIdAsync(usrId)
                ?? throw new InvalidOperationException($"Le createur {usrId} d'une soumission en attente est introuvable.");
            users.Add(usrId, user);
        }

        var players = new List<Player>(dtos.Count);
        foreach (var p in dtos)
        {
            var pInfo = await _playerHandler
                .GetPlayerFullInfoAsync(p);

            players.Add(new Player(pInfo, users.Values));
        }

        return players;
    }

    /// <inheritdoc />
    public async Task<(PlayerSubmissionErrors, ulong, IReadOnlyCollection<Badges>)> ValidatePlayerSubmissionAsync(PlayerSubmissionValidationRequest request)
    {
        var badges = new List<Badges>();

        var p = await _playerRepository
            .GetPlayerByIdAsync(request.PlayerId);

        if (p == null)
            return (PlayerSubmissionErrors.PlayerNotFound, 0, badges);

        if (p.PublicationDate.HasValue || p.RejectDate.HasValue)
            return (PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused, 0, badges);

        if (request.IsAccepted)
        {
            await AcceptSubmittedPlayerAsync(
                    request, p.Clue, p.EasyClue);

            badges.Add(Badges.DoItYourself);

            var players = await _playerRepository
                .GetPlayersByCreatorAsync(p.CreationUserId, true);

            if (players.Count == 5)
                badges.Add(Badges.WeAreKikole);

            // TODO: notify (+ badge)
        }
        else
        {
            await _playerRepository
                .RefusePlayerProposalAsync(request.PlayerId);

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
            .GetPlayersOfTheDayAsync(_clock.Tomorrow, null))
            .OrderBy(_ => _randomizer.Next())
            .ToList();

        var i = 0;
        foreach (var p in randomizedPlayers)
        {
            await _playerRepository
                .ChangePlayerPublicationDateAsync(p.Id, _clock.Tomorrow.AddDays(i));
            i++;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CanDisplayHiddenPlayerAsync(ulong userId)
    {
        var leaderFound = await _leaderRepository
            .GetUserLeadersAsync(_gameCalendar.HiddenDate, _gameCalendar.HiddenDate, false, userId);

        if (leaderFound.Count > 0)
        {
            return true;
        }

        var createdPlayers = await _playerRepository
            .GetPlayersByCreatorAsync(userId, true);

        var leaders = await _leaderRepository
            .GetUserLeadersAsync(_gameCalendar.FirstDate, null, false, userId);

        var countToFind = (_clock.Today - _gameCalendar.FirstDate).Days + 1;

        var createdCount = createdPlayers.Count(_ => _.PublicationDate <= _clock.Today);

        return leaders.Count + createdCount == countToFind;
    }

    private async Task<DateTime> GetNextDateAsync()
    {
        var latestDate = await _playerRepository
            .GetLatestPlayerDateAsync();

        return latestDate.AddDays(1).Date;
    }

    private async Task InsertLanguageCluesAsync(IReadOnlyDictionary<Languages, string?>? clues,
        ulong playerId, bool isEasy)
    {
        var languagesClues = clues?
            .Where(_ => !string.IsNullOrWhiteSpace(_.Value))
            .ToDictionary(_ => (ulong)_.Key, _ => _.Value!.Trim())
            ?? [];

        if (languagesClues.Count > 0)
        {
            await _playerRepository
                .InsertPlayerCluesByLanguageAsync(playerId, (byte)(isEasy ? 1 : 0), languagesClues);
        }
    }

    private async Task UpdateCluesInternalAsync(ulong playerId,
        string clueEn,
        string easyClueEn,
        IReadOnlyDictionary<Languages, string?>? clueLanguages,
        IReadOnlyDictionary<Languages, string?>? easyClueLanguages)
    {
        await _playerRepository
            .UpdatePlayerCluesAsync(playerId, clueEn, easyClueEn);

        await InsertLanguageCluesAsync(
                clueLanguages, playerId, false);

        await InsertLanguageCluesAsync(
                easyClueLanguages, playerId, true);
    }
}
