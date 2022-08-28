using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using KikoleSite.Api.Interfaces;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Models;
using KikoleSite.Elite.Models.Integration;
using KikoleSite.Elite.Repositories;

namespace KikoleSite.Elite.Providers
{
    public sealed class IntegrationProvider : IIntegrationProvider
    {
        private readonly IWriteRepository _writeRepository;
        private readonly IReadRepository _readRepository;
        private readonly ITheEliteWebSiteParser _siteParser;
        private readonly IClock _clock;

        public IntegrationProvider(
            IWriteRepository writeRepository,
            IReadRepository readRepository,
            ITheEliteWebSiteParser siteParser,
            IClock clock)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _siteParser = siteParser;
            _clock = clock;
        }

        public async Task<RefreshPlayersResult> RefreshPlayersAsync(bool addTimesForNewPlayers)
        {
            var errors = new List<string>();

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

            var createdPlayers = new List<PlayerDto>();
            foreach (var pUrl in allPlayerUrls)
            {
                if (!validPlayers.Any(p => p.IsSame(pUrl)))
                {
                    try
                    {
                        var pInfo = await _siteParser
                            .GetPlayerInformationAsync(pUrl, Player.DefaultPlayerHexColor)
                            .ConfigureAwait(false);

                        if (pInfo != null)
                        {
                            var formelyBannedPlayer = bannedPlayers.FirstOrDefault(p => p.IsSame(pUrl));

                            if (formelyBannedPlayer != null)
                            {
                                pInfo.Id = formelyBannedPlayer.Id;
                            }
                            else
                            {
                                var pid = await _writeRepository
                                    .InsertPlayerAsync(pUrl, Player.DefaultPlayerHexColor)
                                    .ConfigureAwait(false);

                                pInfo.Id = pid;
                            }

                            await _writeRepository
                                .UpdatePlayerAsync(pInfo)
                                .ConfigureAwait(false);

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
                var (_, geLogs) = await RefreshPlayersEntriesAsync(
                        Game.GoldenEye, createdPlayers)
                    .ConfigureAwait(false);

                var (_, pdLogs) = await RefreshPlayersEntriesAsync(
                        Game.PerfectDark, createdPlayers)
                    .ConfigureAwait(false);

                errors.AddRange(geLogs);
                errors.AddRange(pdLogs);
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

            var (entriesCount, errors) = await RefreshPlayersEntriesAsync(
                    game, validPlayers)
                .ConfigureAwait(false);

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
            var groupsCount = new ConcurrentBag<int>();
            var tasks = new List<Task>(parallel);
            var groupSize = allEntries.Count / (parallel - 1);
            for (var i = 0; i < parallel; i++)
            {
                tasks.Add(ManageGroupOfEntriesAsync(
                    validPlayers,
                    allEntries.Skip(i * groupSize).Take(groupSize),
                    existingEntries,
                    groupsErrors,
                    groupsCount));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return new RefreshEntriesResult
            {
                Errors = groupsErrors.ToList(),
                ReplacedEntriesCount = groupsCount.Sum()
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

            var (localCount, localErrors) = await ExtractPlayerTimesAsync(
                    Game.GoldenEye, p)
                .ConfigureAwait(false);

            errors.AddRange(localErrors);
            count += localCount;

            (localCount, localErrors) = await ExtractPlayerTimesAsync(
                    Game.PerfectDark, p)
                .ConfigureAwait(false);

            errors.AddRange(localErrors);
            count += localCount;

            return new RefreshEntriesResult
            {
                Errors = errors,
                ReplacedEntriesCount = count
            };
        }

        private async Task<(int count, IReadOnlyCollection<string> errors)> RefreshPlayersEntriesAsync(Game game, IReadOnlyCollection<PlayerDto> validPlayers)
        {
            var errors = new ConcurrentBag<string>();
            var count = 0;

            const int parallel = 8;
            for (var i = 0; i < validPlayers.Count; i += parallel)
            {
                await Task.WhenAll(validPlayers.Skip(i).Take(parallel).Select(async player =>
                {
                    try
                    {
                        var (localCount, localErrors) = await ExtractPlayerTimesAsync(
                                game, player)
                            .ConfigureAwait(false);
                        errors.AddRange(localErrors);
                        count += localCount;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error while adding {game} time entries for {player.Id}.\n{ex.Message}");
                    }
                })).ConfigureAwait(false);
            }

            return (count, errors);
        }

        private async Task ManageGroupOfEntriesAsync(
            IReadOnlyCollection<PlayerDto> validPlayers,
            IEnumerable<EntryWebDto> entriesToManage,
            List<(long PlayerId, Stage Stage, Level Level, long Time, Engine Engine)> existingEntries,
            ConcurrentBag<string> groupsErrors,
            ConcurrentBag<int> groupsCount)
        {
            var errors = new List<string>();
            var count = 0;

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
                            count++;
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
            groupsCount.Add(count);
        }

        private async Task<(int count, IReadOnlyCollection<string> errors)> ExtractPlayerTimesAsync(Game game, PlayerDto player)
        {
            var count = 0;
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
                        await _writeRepository
                            .ReplaceTimeEntryAsync(entry.ToEntry(player.Id))
                            .ConfigureAwait(false);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error while processing entry {entry}\n{ex.Message}");
                    }
                }

                var removeEntriesId = entriesFromDatabase
                    .Where(e => !entriesFromSite.Any(end => end.AreSame(e)))
                    .Select(e => e.Id)
                    .ToArray();

                await _writeRepository
                    .DeleteEntriesAsync(removeEntriesId)
                    .ConfigureAwait(false);
            }
            else
            {
                errors.Add($"Unable to get {game} entries page for player {player.UrlName}.");
            }

            return (count, errors);
        }
    }
}
