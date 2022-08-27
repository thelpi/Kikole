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
                var geLogs = await RefreshPlayersEntriesAsync(
                        Game.GoldenEye, createdPlayers)
                    .ConfigureAwait(false);

                var pdLogs = await RefreshPlayersEntriesAsync(
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

        public async Task RefreshAllEntriesAsync(Game game)
        {
            var validPlayers = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            await RefreshPlayersEntriesAsync(game, validPlayers).ConfigureAwait(false);
        }

        public async Task RefreshEntriesToDateAsync(DateTime stopAt)
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
                .GetEntriesAsync(null, null, new DateTime(stopAt.Year, stopAt.Month, 1), _clock.Tomorrow)
                .ConfigureAwait(false))
                .GroupBy(_ => (_.PlayerId, _.Stage, _.Level, _.Time, _.Engine))
                .Select(_ => _.Key)
                .ToList();

            var tasks = new List<Task>(parallel);
            var groupSize = allEntries.Count / (parallel - 1);
            for (var i = 0; i < parallel; i++)
            {
                tasks.Add(
                    ManageGroupOfEntriesAsync(
                        validPlayers,
                        allEntries.Skip(i * groupSize).Take(groupSize),
                        existingEntries));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task<IReadOnlyCollection<string>> RefreshPlayersEntriesAsync(Game game, IReadOnlyCollection<PlayerDto> validPlayers)
        {
            var logs = new ConcurrentBag<string>();

            const int parallel = 8;
            for (var i = 0; i < validPlayers.Count; i += parallel)
            {
                await Task.WhenAll(validPlayers.Skip(i).Take(parallel).Select(async player =>
                {
                    try
                    {
                        await ExtractPlayerTimesAsync(game, player).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logs.Add($"Error while adding {game} time entries for {player.Id}.\n{ex.Message}");
                    }
                })).ConfigureAwait(false);
            }

            return logs;
        }

        private async Task ManageGroupOfEntriesAsync(
            IReadOnlyCollection<PlayerDto> validPlayers,
            IEnumerable<EntryWebDto> entriesToManage,
            List<(long PlayerId, Stage Stage, Level Level, long Time, Engine Engine)> existingEntries)
        {
            foreach (var entry in entriesToManage)
            {
                if (entry.Date.Value >= _clock.Tomorrow)
                    continue;

                var idMatch = validPlayers
                    .FirstOrDefault(p =>
                        p.IsSame(entry.PlayerUrlName))
                    ?.Id;

                if (idMatch.HasValue)
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
                    }
                }
            }
        }

        private async Task ExtractPlayerTimesAsync(Game game, PlayerDto player)
        {
            var entries = await _siteParser
                .GetPlayerEntriesAsync(game, player.UrlName)
                .ConfigureAwait(false);

            if (entries != null)
            {
                await _writeRepository
                    .DeletePlayerEntriesAsync(game, player.Id)
                    .ConfigureAwait(false);

                var groupEntries = entries.GroupBy(e => (e.Stage, e.Level, e.Time, e.Engine));
                foreach (var group in groupEntries)
                {
                    var groupEntry = group.OrderBy(d => d.Date ?? DateTime.MaxValue).First();
                    await _writeRepository
                        .ReplaceTimeEntryAsync(groupEntry.ToEntry(player.Id))
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
