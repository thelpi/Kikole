using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using KikoleSite.Elite.Configurations;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;
using KikoleSite.Elite.Models;
using KikoleSite.Elite.Models.Integration;
using KikoleSite.Elite.Repositories;
using KikoleSite.Interfaces;
using Microsoft.Extensions.Options;

namespace KikoleSite.Elite.Providers
{
    public sealed class IntegrationProvider : IIntegrationProvider
    {
        private readonly IWriteRepository _writeRepository;
        private readonly IReadRepository _readRepository;
        private readonly ITheEliteWebSiteParser _siteParser;
        private readonly IClock _clock;
        private readonly RankingConfiguration _configuration;

        public IntegrationProvider(
            IWriteRepository writeRepository,
            IReadRepository readRepository,
            ITheEliteWebSiteParser siteParser,
            IClock clock,
            IOptions<RankingConfiguration> configuration)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _siteParser = siteParser;
            _clock = clock;
            _configuration = configuration.Value;
        }

        public async Task<RefreshPlayersResult> RefreshPlayersAsync(
            bool addTimesForNewPlayers,
            bool refreshExistingPlayers)
        {
            var errors = new ConcurrentBag<string>();

            var gePlayerUrls = await _siteParser
                .GetPlayerUrlsAsync(Game.GoldenEye)
                .ConfigureAwait(false);

            var pdPlayerUrls = await _siteParser
                .GetPlayerUrlsAsync(Game.PerfectDark)
                .ConfigureAwait(false);

            if (gePlayerUrls.Count == 0 || pdPlayerUrls.Count == 0)
            {
                errors.Add("Process canceled: fails to get players list from the website.");
                return new RefreshPlayersResult
                {
                    Errors = errors
                };
            }

            var allPlayerUrls = gePlayerUrls
                .Concat(pdPlayerUrls)
                .Distinct()
                .Select(pUrl => HttpUtility.UrlDecode(pUrl))
                .ToList();

            var validPlayers = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var bannedPlayers = await _readRepository
                .GetPlayersAsync(true)
                .ConfigureAwait(false);

            var createdPlayers = new ConcurrentBag<PlayerDto>();

            const int Concurrency = 4;
            var countByGroup = allPlayerUrls.Count / Concurrency;
            if (allPlayerUrls.Count % Concurrency > 0)
                countByGroup++;

            var tasks = new List<Task>();
            for (var i = 0; i < Concurrency; i++)
            {
                var groupOfPlayerUrls = allPlayerUrls.Skip(i * countByGroup).Take(countByGroup);
                tasks.Add(Task.Run(async () =>
                {
                    foreach (var pUrl in groupOfPlayerUrls)
                    {
                        var matchingPlayer = validPlayers.SingleOrDefault(p => p.IsSame(pUrl));
                        if (matchingPlayer == null || refreshExistingPlayers)
                        {
                            try
                            {
                                var pInfo = await _siteParser
                                    .GetPlayerInformationAsync(pUrl, Player.DefaultPlayerHexColor)
                                    .ConfigureAwait(false);

                                if (pInfo != null)
                                {
                                    if (matchingPlayer == null)
                                    {
                                        var formelyBannedPlayer = bannedPlayers.FirstOrDefault(p => p.IsSame(pUrl));

                                        if (formelyBannedPlayer != null)
                                        {
                                            pInfo.Id = formelyBannedPlayer.Id;
                                        }
                                        else
                                        {
                                            var pId = await _writeRepository
                                                .InsertPlayerAsync(pUrl, Player.DefaultPlayerHexColor)
                                                .ConfigureAwait(false);

                                            pInfo.Id = pId;
                                        }
                                    }
                                    else
                                    {
                                        pInfo.Id = matchingPlayer.Id;
                                    }

                                    await _writeRepository
                                        .UpdatePlayerAsync(pInfo.WithRealYearOfBirth(matchingPlayer))
                                        .ConfigureAwait(false);

                                    if (matchingPlayer == null)
                                        createdPlayers.Add(pInfo);
                                }
                                else
                                {
                                    errors.Add($"No page found for player with URI {pUrl}.");
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Error while checking player with URI {pUrl}.\n{ex.Message}");
                            }
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            var playersToBan = validPlayers
                .Where(p => !allPlayerUrls.Any(pUrl => p.IsSame(pUrl)))
                .ToList();

            foreach (var pToBan in playersToBan)
            {
                try
                {
                    await _writeRepository
                        .DeletePlayerEntriesAsync(Game.GoldenEye, pToBan.Id)
                        .ConfigureAwait(false);

                    await _writeRepository
                        .DeletePlayerEntriesAsync(Game.PerfectDark, pToBan.Id)
                        .ConfigureAwait(false);

                    await _writeRepository
                        .BanPlayerAsync(pToBan.Id)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    errors.Add($"Error while deleting {pToBan.Id}.\n{ex.Message}");
                }
            }

            if (addTimesForNewPlayers && createdPlayers.Count > 0)
            {
                var (_, geUpdatedEntries, geLogs) = await RefreshPlayersEntriesAsync(
                        Game.GoldenEye, createdPlayers)
                    .ConfigureAwait(false);

                var (_, pdUpdateEntries, pdLogs) = await RefreshPlayersEntriesAsync(
                        Game.PerfectDark, createdPlayers)
                    .ConfigureAwait(false);

                errors.AddRange(geLogs);
                errors.AddRange(pdLogs);

                var allEntriesUpdated = geUpdatedEntries
                    .Concat(pdUpdateEntries)
                    .ToList();

                await RegenerateRankingsAsync(allEntriesUpdated).ConfigureAwait(false);
            }

            return new RefreshPlayersResult
            {
                CreatedPlayers = createdPlayers,
                BannedPlayers = playersToBan,
                Errors = errors
            };
        }

        public async Task<RefreshEntriesResult> RefreshAllEntriesAsync(Game game)
        {
            var validPlayers = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var (entriesCount, updatedEntries, errors) = await RefreshPlayersEntriesAsync(
                    game, validPlayers)
                .ConfigureAwait(false);

            await RegenerateRankingsAsync(updatedEntries).ConfigureAwait(false);

            return new RefreshEntriesResult
            {
                Errors = errors,
                ReplacedEntriesCount = entriesCount
            };
        }

        public async Task<RefreshEntriesResult> RefreshEntriesToDateAsync(DateTime stopAt)
        {
            var validPlayers = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var allDatesToLoop = stopAt.LoopBetweenDates(DateStep.Month, _clock).Reverse().ToList();

            var allEntries = new ConcurrentBag<EntryWebDto>();

            const int parallel = 8;
            for (var i = 0; i < allDatesToLoop.Count; i += parallel)
            {
                await Task.WhenAll(allDatesToLoop.Skip(i).Take(parallel).Select(async loopDate =>
                {
                    var results = await _siteParser
                        .GetMonthPageTimeEntriesAsync(loopDate.Year, loopDate.Month)
                        .ConfigureAwait(false);

                    allEntries.AddRange(results);
                })).ConfigureAwait(false);
            }

            var existingEntries = (await _readRepository
                .GetEntriesAsync(null, null, stopAt.Truncat(DateStep.Month), _clock.Tomorrow)
                .ConfigureAwait(false))
                .GroupBy(_ => (_.PlayerId, _.Stage, _.Level, _.Time, _.Engine))
                .Select(_ => _.Key)
                .ToList();

            var groupsErrors = new ConcurrentBag<string>();
            var newEntries = new ConcurrentBag<EntryDto>();
            var tasks = new List<Task>(parallel);
            var groupSize = allEntries.Count / (parallel - 1);
            for (var i = 0; i < parallel; i++)
            {
                tasks.Add(ManageGroupOfEntriesAsync(
                    validPlayers,
                    allEntries.Skip(i * groupSize).Take(groupSize),
                    existingEntries,
                    groupsErrors,
                    newEntries));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await RegenerateRankingsAsync(newEntries).ConfigureAwait(false);

            return new RefreshEntriesResult
            {
                Errors = groupsErrors.ToList(),
                ReplacedEntriesCount = newEntries.Count
            };
        }

        public async Task<RefreshEntriesResult> RefreshPlayerEntriesAsync(long playerId)
        {
            var errors = new ConcurrentBag<string>();
            var count = 0;

            var players = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var p = players.SingleOrDefault(_ => _.Id == playerId);
            if (p == null)
            {
                errors.Add($"The player identifier {playerId} doesn't exist.");
                return new RefreshEntriesResult
                {
                    Errors = errors,
                    ReplacedEntriesCount = count
                };
            }

            var (newEntriesGe, deletedEntriesGe, localErrorsGe) = await ExtractPlayerTimesAsync(
                    Game.GoldenEye, p)
                .ConfigureAwait(false);

            errors.AddRange(localErrorsGe);
            count += newEntriesGe.Count;

            var (newEntriesPd, deletedEntriesPd, localErrorsPd) = await ExtractPlayerTimesAsync(
                    Game.PerfectDark, p)
                .ConfigureAwait(false);

            errors.AddRange(localErrorsPd);
            count += newEntriesPd.Count;

            var allEntriesUpdated = newEntriesPd
                .Concat(deletedEntriesPd)
                .Concat(newEntriesGe)
                .Concat(deletedEntriesGe)
                .ToList();

            await RegenerateRankingsAsync(allEntriesUpdated).ConfigureAwait(false);

            return new RefreshEntriesResult
            {
                Errors = errors,
                ReplacedEntriesCount = count
            };
        }

        private async Task<(int count, IReadOnlyCollection<EntryDto> updatedEntries, IReadOnlyCollection<string> errors)> RefreshPlayersEntriesAsync(Game game, IReadOnlyCollection<PlayerDto> validPlayers)
        {
            var errors = new ConcurrentBag<string>();
            var updatedEntries = new ConcurrentBag<EntryDto>();
            var count = 0;

            const int parallel = 8;
            for (var i = 0; i < validPlayers.Count; i += parallel)
            {
                await Task.WhenAll(validPlayers.Skip(i).Take(parallel).Select(async player =>
                {
                    try
                    {
                        var (newEntries, deletedEntries, localErrors) = await ExtractPlayerTimesAsync(
                                game, player)
                            .ConfigureAwait(false);
                        errors.AddRange(localErrors);
                        updatedEntries.AddRange(newEntries);
                        updatedEntries.AddRange(deletedEntries);
                        count += newEntries.Count;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error while adding {game} time entries for {player.Id}.\n{ex.Message}");
                    }
                })).ConfigureAwait(false);
            }

            return (count, updatedEntries, errors);
        }

        private async Task ManageGroupOfEntriesAsync(
            IReadOnlyCollection<PlayerDto> validPlayers,
            IEnumerable<EntryWebDto> entriesToManage,
            List<(long PlayerId, Stage Stage, Level Level, long Time, Engine Engine)> existingEntries,
            ConcurrentBag<string> groupsErrors,
            ConcurrentBag<EntryDto> newEntries)
        {
            var errors = new List<string>();

            foreach (var entry in entriesToManage)
            {
                if (entry.Date.Value >= _clock.Tomorrow)
                {
                    errors.Add($"Entry {entry} is in the future: {entry.Date.Value}.");
                    continue;
                }

                var idMatch = validPlayers
                    .FirstOrDefault(p =>
                        p.IsSame(entry.PlayerUrlName))
                    ?.Id;

                if (idMatch.HasValue)
                {
                    try
                    {
                        var dto = entry.ToEntry(idMatch.Value);
                        dto.Engine = await _siteParser
                            .GetTimeEntryEngineAsync(entry.EngineUrl)
                            .ConfigureAwait(false);

                        if (!existingEntries.Contains((dto.PlayerId, dto.Stage, dto.Level, dto.Time, dto.Engine)))
                        {
                            await _writeRepository
                                .ReplaceTimeEntryAsync(dto)
                                .ConfigureAwait(false);
                            newEntries.Add(dto);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error while processing entry {entry}\n{ex.Message}");
                    }
                }
                else
                {
                    errors.Add($"Entry {entry} has no matching valid player.");
                }
            }

            groupsErrors.AddRange(errors);
        }

        private async Task<(IReadOnlyCollection<EntryDto> newEntries, IReadOnlyCollection<EntryDto> deletedEntries, IReadOnlyCollection<string> errors)> ExtractPlayerTimesAsync(Game game, PlayerDto player)
        {
            var newEntries = new List<EntryDto>();
            var deletedEntries = new List<EntryDto>();
            var errors = new List<string>();

            var entriesFromSite = await _siteParser
                .GetPlayerEntriesAsync(game, player.UrlName)
                .ConfigureAwait(false);

            var entriesFromDatabase = await _readRepository
                .GetPlayerEntriesAsync(player.Id, game)
                .ConfigureAwait(false);

            if (entriesFromSite != null)
            {
                entriesFromSite = entriesFromSite
                    .GroupBy(e => (e.Stage, e.Level, e.Time, e.Engine))
                    .Select(e => e.OrderBy(d => d.Date ?? DateTime.MaxValue).First())
                    .ToList();

                var entriesToReplace = entriesFromSite
                    .Where(e => !entriesFromDatabase.Any(ce => ce.AreEqual(e)))
                    .ToList();

                foreach (var entry in entriesToReplace)
                {
                    try
                    {
                        var dto = entry.ToEntry(player.Id);
                        await _writeRepository
                            .ReplaceTimeEntryAsync(dto)
                            .ConfigureAwait(false);
                        newEntries.Add(dto);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error while processing entry {entry}\n{ex.Message}");
                    }
                }

                deletedEntries = entriesFromDatabase
                    .Where(e => !entriesFromSite.Any(end => end.AreSame(e)))
                    .ToList();

                await _writeRepository
                    .DeleteEntriesAsync(deletedEntries.Select(e => e.Id).ToArray())
                    .ConfigureAwait(false);
            }
            else
            {
                errors.Add($"Unable to get {game} entries page for player {player.UrlName}.");
            }

            return (newEntries, deletedEntries, errors);
        }

        private async Task RegenerateRankingsAsync(IReadOnlyCollection<EntryDto> entries)
        {
#if DEBUG
            var oldestEntryByStageLevel = entries
                .GroupBy(x => (x.Stage, x.Level))
                .ToDictionary(x => x.Key, x => x.Min(_ => _.Date ?? _.Stage.GetGame().GetEliteFirstDate()));

            foreach (var (stage, level) in oldestEntryByStageLevel.Keys)
            {
                var oldestDate = oldestEntryByStageLevel[(stage, level)];

                await _writeRepository
                    .RemoveRankingsAfterDateAsync(stage, level, oldestDate)
                    .ConfigureAwait(false);

                var stageLevelEntries = (await _readRepository
                    .GetEntriesAsync(stage, level, null, null)
                    .ConfigureAwait(false))
                    .ToList();

                stageLevelEntries.ManageDateLessEntries(_configuration.NoDateEntryRankingRule, _clock.Now);

                var dates = stageLevelEntries
                    .Where(x => x.Date >= oldestDate)
                    .Select(x => x.Date.Value);

                foreach (var date in dates)
                {
                    // keeps the best time for each player
                    var entriesAtDate = stageLevelEntries
                        .Where(x => x.Date <= date)
                        .GroupBy(x => x.PlayerId)
                        .Select(x => x.OrderBy(x => x.Time).ThenBy(x => x.Date).First())
                        .OrderBy(x => x.Time)
                        .ToList();

                    // compute points
                    long? time = null;
                    var playersCountForTime = 1;
                    var points = StageLeaderboard.BasePoints;
                    var rank = 0;
                    foreach (var entry in entriesAtDate)
                    {
                        if (!time.HasValue || time != entry.Time)
                        {
                            if (time.HasValue)
                            {
                                for (var i = 0; i < playersCountForTime; i++)
                                    points = StageLeaderboard.PointsChart.TryGetValue(points, out int tmpPoints) ? tmpPoints : points - 1;
                            }
                            rank += playersCountForTime;
                            playersCountForTime = 1;
                            time = entry.Time;
                        }
                        else
                            playersCountForTime++;

                        var rk = new StageLevelRankingDto
                        {
                            Date = date,
                            Level = level,
                            PlayerId = entry.PlayerId,
                            Stage = stage,
                            Time = (int)entry.Time,
                            Points = points,
                            Rank = rank
                        };

                        await _writeRepository
                            .ReplaceRankingAsync(rk)
                            .ConfigureAwait(false);
                    }
                }
            }
#endif
        }
    }
}
