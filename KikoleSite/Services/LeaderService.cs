using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Handlers;
using KikoleSite.Helpers;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Services;

/// <summary>
/// Leader service implementation.
/// </summary>
/// <seealso cref="ILeaderService"/>
public class LeaderService : ILeaderService
{
    private const int PodiumSize = 3;

    private readonly IPlayerRepository _playerRepository;
    private readonly ILeaderRepository _leaderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProposalRepository _proposalRepository;
    private readonly IClock _clock;
    private readonly IGameCalendar _gameCalendar;
    private readonly IStringLocalizer<Translations> _resources;
    private readonly IPlayerHandler _playerHandler;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
    /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
    /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
    /// <param name="proposalRepository">Instance of <see cref="IProposalRepository"/>.</param>
    /// <param name="resources">Instance of <see cref="IStringLocalizer"/>.</param>
    /// <param name="playerHandler">Instance of <see cref="IPlayerHandler"/>.</param>
    /// <param name="clock">Clock service.</param>
    /// <param name="gameCalendar">Instance of <see cref="IGameCalendar"/>.</param>
    public LeaderService(IPlayerRepository playerRepository,
        ILeaderRepository leaderRepository,
        IUserRepository userRepository,
        IProposalRepository proposalRepository,
        IClock clock,
        IGameCalendar gameCalendar,
        IStringLocalizer<Translations> resources,
        IPlayerHandler playerHandler)
    {
        _playerRepository = playerRepository;
        _leaderRepository = leaderRepository;
        _userRepository = userRepository;
        _proposalRepository = proposalRepository;
        _clock = clock;
        _gameCalendar = gameCalendar;
        _resources = resources;
        _playerHandler = playerHandler;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<LeaderboardItem>> GetLeaderboardAsync(DateTime startDate, DateTime endDate, LeaderSorts leaderSort)
    {
        if (startDate.Date > endDate.Date)
        {
            var tmp = endDate;
            endDate = startDate;
            startDate = tmp;
        }

        var onTimeOnly = leaderSort != LeaderSorts.SuccessCountOverall
            && leaderSort != LeaderSorts.TotalPointsOverall;

        var items = await ComputeLeaderboardItemsAsync(
                startDate, endDate, onTimeOnly);

        switch (leaderSort)
        {
            case LeaderSorts.BestTime:
                items = items.SetPositions(_ => _.BestTime, false, (_, r) => _.Rank = r);
                break;
            case LeaderSorts.SuccessCountOverall:
            case LeaderSorts.SuccessCount:
                items = items.SetPositions(_ => _.KikolesFound, true, (_, r) => _.Rank = r);
                break;
            case LeaderSorts.TotalPointsOverall:
            case LeaderSorts.TotalPoints:
                items = items.SetPositions(_ => _.Points, true, (_, r) => _.Rank = r);
                break;
        }

        return items;
    }

    private async Task<List<LeaderboardItem>> ComputeLeaderboardItemsAsync(DateTime startDate, DateTime endDate, bool onTimeOnly)
    {
        var leaders = await _leaderRepository
            .GetLeadersAsync(startDate, endDate, onTimeOnly);

        // we need players to get creators
        var players = await _playerRepository
            .GetPlayersOfTheDayAsync(startDate, endDate);

        // mix of leaders and creators (some might not play during the period)
        var allUsersId = players
            .Select(_ => _.CreationUserId)
            .Concat(leaders.Select(_ => _.UserId))
            .Distinct();

        var users = await GetUsersFromIdsAsync(allUsersId);

        var items = new List<LeaderboardItem>();
        foreach (var user in users)
        {
            var userLeaders = leaders.Where(_ => _.UserId == user.Id);
            var userPlayers = players.Where(_ => _.CreationUserId == user.Id);

            var points = userLeaders.Sum(_ => _.Points);
            foreach (var userPlayer in userPlayers)
                points += ScoreCalculator.SubmissionPoints;

            items.Add(new LeaderboardItem
            {
                BestTime = userLeaders.Any()
                    ? userLeaders.Select(_ => new TimeSpan(0, _.Time, 0)).Min()
                    : new TimeSpan(23, 59, 59),
                KikolesAttempted = await _proposalRepository
                    .GetDaysCountWithProposalAsync(startDate, endDate, user.Id, onTimeOnly),
                KikolesFound = userLeaders.Count(),
                KikolesProposed = userPlayers.Count(),
                Points = points,
                UserId = user.Id,
                UserName = user.Login
            });
        }

        return items;
    }

    /// <inheritdoc />
    public async Task<UserStat?> GetUserStatisticsAsync(ulong userId, ulong requestUserId, string anonymizedName, bool requestUserFoundToday)
    {
        var user = await _userRepository
            .GetUserByIdAsync(userId);

        if (user == null)
            return null;

        var requestUser = await _userRepository
            .GetUserByIdAsync(requestUserId);

        var stats = new List<DailyUserStat>();

        var currentDate = _gameCalendar.FirstDate;

        var stopDate = requestUserFoundToday
            ? _clock.Today
            : _clock.Yesterday;

        var pDays = await _playerRepository
            .GetPlayersOfTheDayAsync(currentDate, stopDate);

        var allProposals = await _proposalRepository
            .GetProposalsAsync(currentDate, stopDate, userId);

        var allLeaders = await _leaderRepository
            .GetLeadersAsync(currentDate, stopDate, false);

        while (currentDate <= stopDate)
        {
            // meme invariante qu'ailleurs : il y a un joueur par jour, et le nom de la
            // date manquante vaut mieux que « Sequence contains no matching element »
            var pDay = pDays.FirstOrDefault(x => x.PublicationDate == currentDate)
                ?? throw new InvalidOperationException(
                    $"Aucun joueur n'est programme pour le {currentDate:yyyy-MM-dd}.");

            var proposals = allProposals.Where(x => x.ProposalDate == currentDate);

            var leaders = allLeaders.Where(x => x.ProposalDate == currentDate);

            var meLeader = leaders.SingleOrDefault(l => l.UserId == userId);

            var isCreator = (currentDate.Date < stopDate || pDay.HideCreator == 0)
                && userId == pDay.CreationUserId;

            var pName = pDay.Name;

            if (requestUserId == 0)
                pName = anonymizedName;
            else if (!leaders.Any(_ => _.UserId == requestUserId) && pDay.CreationUserId != requestUserId)
            {
                if (!(requestUser?.UserTypeId == (ulong)UserTypes.Administrator))
                    pName = anonymizedName;
            }

            var singleStat = isCreator
                ? new DailyUserStat(currentDate, pName, ScoreCalculator.SubmissionPoints)
                : new DailyUserStat(userId, currentDate, pName, proposals.Any(_ => _.IsCurrentDay), proposals.Any(), leaders, meLeader);

            stats.Add(singleStat);
            currentDate = currentDate.AddDays(1);
        }

        return new UserStat(stats, user.Login, user.CreationDate);
    }

    /// <inheritdoc />
    public async Task ComputeMissingLeadersAsync(IReadOnlyDictionary<ulong, ulong> countryContinents)
    {
        var players = await _playerRepository
            .GetPlayersOfTheDayAsync(null, _clock.Today);

        foreach (var playerOfTheDay in players)
        {
            var usersId = await _proposalRepository
                .GetMissingUsersAsLeaderAsync(playerOfTheDay.PublicationDate!.Value);

            var playerInfo = await _playerHandler
                .GetPlayerFullInfoAsync(playerOfTheDay);

            foreach (var userId in usersId)
            {
                var proposals = (await _proposalRepository
                    .GetProposalsAsync(playerOfTheDay.PublicationDate.Value, userId))
                    .OrderBy(p => p.CreationDate)
                    .ToList();

                var winIndex = proposals.FindIndex(p =>
                    p.Successful > 0 && (ProposalTypes)p.ProposalTypeId == ProposalTypes.Name);

                if (winIndex < 0)
                    continue;

                // we had for a while a bug of proposals after the player has been found
                var untilWin = proposals.Take(winIndex + 1);

                // meme calcul que le score affiche en direct, pour que le rattrapage
                // ne puisse pas diverger du barème réel
                ScoreCalculator.GetProposalResponsesWithPoints(
                    untilWin, playerInfo, out var points, _resources, countryContinents);

                var winningProposal = proposals[winIndex];

                await _leaderRepository
                    .CreateLeaderAsync(new LeaderDto
                    {
                        Points = (ushort)points,
                        ProposalDate = playerOfTheDay.PublicationDate.Value,
                        Time = (winningProposal.CreationDate - playerOfTheDay.PublicationDate.Value).ToRoundMinutes(),
                        UserId = userId,
                        CreationDate = winningProposal.CreationDate
                    });
            }
        }
    }

    /// <inheritdoc />
    public async Task<Dayboard> GetDayboardAsync(DateTime day, DayLeaderSorts sort,
        IReadOnlyDictionary<ulong, ulong> countryContinents)
    {
        day = day.Date;

        var leaders = await _leaderRepository
            .GetLeadersAtDateAsync(day, false);

        var proposals = await _proposalRepository
            .GetProposalsAsync(day, false);

        var player = await _playerHandler
            .GetPlayerOfTheDayFullInfoAsync(day);

        var leaderUsers = leaders.Select(_ => _.UserId);

        var allUsersId = leaderUsers
            .Concat(proposals.Select(_ => _.UserId))
            .Append(player.Player.CreationUserId)
            .Distinct();

        var users = (await GetUsersFromIdsAsync(allUsersId))
            .ToDictionary(_ => _.Id, _ => _);

        var leaderItems = leaders
            .Select(_ => new DayboardLeaderItem
            {
                Date = _.CreationDate.Date,
                IsCreator = false,
                Points = _.Points,
                Time = new TimeSpan(0, _.Time, 0),
                UserId = _.UserId,
                UserName = users[_.UserId].Login
            });

        if (users.ContainsKey(player.Player.CreationUserId))
        {
            leaderItems = leaderItems.Append(new DayboardLeaderItem
            {
                Date = day,
                IsCreator = true,
                Points = ScoreCalculator.SubmissionPoints,
                Time = new TimeSpan(23, 59, 59),
                UserId = player.Player.CreationUserId,
                UserName = users[player.Player.CreationUserId].Login
            });
        }

        switch (sort)
        {
            case DayLeaderSorts.BestTime:
                leaderItems = leaderItems.SetPositions(_ => _.Time, false, (_, r) => _.Rank = r);
                break;
            case DayLeaderSorts.TotalPoints:
                leaderItems = leaderItems.SetPositions(_ => _.Points, true, (_, r) => _.Rank = r);
                break;
        }

        var searchers = new List<DayboardSearcherItem>(proposals.Count);
        foreach (var propUserGroup in proposals
            .Where(_ => !leaderUsers.Contains(_.UserId))
            .GroupBy(_ => _.UserId))
        {
            var dsi = new DayboardSearcherItem
            {
                Date = propUserGroup.Select(p => p.CreationDate).Min().Date,
                LastActivity = propUserGroup.Select(p => p.CreationDate).Max(),
                UserId = propUserGroup.Key,
                UserName = users[propUserGroup.Key].Login
            };

            ScoreCalculator.GetProposalResponsesWithPoints(propUserGroup, player, out var points, _resources, countryContinents);
            dsi.Points = points;

            searchers.Add(dsi);
        }

        return new Dayboard
        {
            Date = day,
            Sort = sort,
            Searchers = searchers.OrderBy(_ => _.Date).ToList(),
            Leaders = leaderItems.ToList()
        };
    }

    public async Task<Podiums> GetPodiumsAsync()
    {
        var months = new Dictionary<(int month, int year), (User first, User second, User third)>();

        var users = new Dictionary<ulong, (User, int, int, int)>();

        var date = _gameCalendar.FirstMonth;

        var currentMonth = _clock.FirstOfMonth;
        while (date <= currentMonth)
        {
            var nextMonth = date.AddMonths(1);

            var ldItems = await ComputeLeaderboardItemsAsync(
                    new DateTime(date.Year, date.Month, 1),
                    date == currentMonth ? _clock.Yesterday : nextMonth.AddDays(-1),
                    true);

            var orderedLdItems = ldItems
                .OrderByDescending(x => x.Points)
                .ThenByDescending(x => x.KikolesFound)
                .ThenBy(x => x.BestTime)
                .ToList();

            // les medailles ne sont distribuees qu'une fois le podium complet : sinon
            // un mois comptant moins de trois joueurs classes serait ecarte de la liste
            // des podiums mensuels tout en creditant le cumul global, et les deux
            // tableaux de la page Leaderboard afficheraient des totaux incoherents
            if (orderedLdItems.Count >= PodiumSize)
            {
                months.Add((date.Month, date.Year), (
                    CreditPodiumPosition(users, orderedLdItems[0], 0),
                    CreditPodiumPosition(users, orderedLdItems[1], 1),
                    CreditPodiumPosition(users, orderedLdItems[2], 2)));
            }

            date = nextMonth;
        }

        return new Podiums
        {
            MonthlyPodiums = months,
            OverallPodium = users.Values
                .Select(x => x)
                .OrderByDescending(x => x.Item2)
                .ThenByDescending(x => x.Item3)
                .ThenByDescending(x => x.Item4)
                .ToList()
        };
    }

    /// <summary>
    /// Credite au cumul global la medaille correspondant a la position occupee
    /// (0 = or, 1 = argent, 2 = bronze), en creant l'utilisateur s'il est inconnu.
    /// N'est appelee que sur un podium complet.
    /// </summary>
    private static User CreditPodiumPosition(
        Dictionary<ulong, (User, int, int, int)> users, LeaderboardItem item, int position)
    {
        var golds = position == 0 ? 1 : 0;
        var silvers = position == 1 ? 1 : 0;
        var bronzes = position == 2 ? 1 : 0;

        if (!users.TryGetValue(item.UserId, out var known))
        {
            var newUser = new User(item.UserId, item.UserName);
            users.Add(item.UserId, (newUser, golds, silvers, bronzes));
            return newUser;
        }

        users[item.UserId] = (
            known.Item1,
            known.Item2 + golds,
            known.Item3 + silvers,
            known.Item4 + bronzes);

        return known.Item1;
    }

    private async Task<List<UserDto>> GetUsersFromIdsAsync(IEnumerable<ulong> allUsersId)
    {
        IReadOnlyCollection<ulong> ids = allUsersId as IReadOnlyCollection<ulong> ?? [.. allUsersId];

        if (ids.Count == 0)
            return [];

        var users = await _userRepository
            .GetUsersByIdsAsync(ids);

        if (users.Count != ids.Count)
        {
            var foundIds = users.Select(u => u.Id).ToHashSet();
            var missingIds = ids.Where(id => !foundIds.Contains(id));
            throw new InvalidOperationException($"Utilisateur(s) introuvable(s) : {string.Join(", ", missingIds)}.");
        }

        return [.. users.Where(u => u.UserTypeId != (ulong)UserTypes.Administrator)];
    }

}
