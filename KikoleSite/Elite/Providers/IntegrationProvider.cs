using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;
using KikoleSite.Elite.Models;
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

        public async Task ScanAllPlayersEntriesHistoryAsync(Game game)
        {
            var (validPlayers, bannedPlayers) = await GetPlayersAsync().ConfigureAwait(false);

            const int parallel = 8;
            for (var i = 0; i < validPlayers.Count; i += parallel)
            {
                await Task.WhenAll(validPlayers.Skip(i).Take(parallel).Select(async player =>
                {
                    await ExtractPlayerTimesAsync(game, player)
                        .ConfigureAwait(false);
                })).ConfigureAwait(false);
            }
        }

        public async Task ScanPlayerEntriesHistoryAsync(Game game, long playerId)
        {
            var (validPlayers, bannedPlayers) = await GetPlayersAsync().ConfigureAwait(false);

            var player = validPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                throw new ArgumentException($"invalid {nameof(playerId)}.", nameof(playerId));
            }

            await ExtractPlayerTimesAsync(game, player).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<Player>> GetCleanableDirtyPlayersAsync()
        {
            var okPlayers = new System.Collections.Concurrent.ConcurrentBag<Player>();

            var players = await _readRepository
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            const int parallel = 8;
            for (var i = 0; i < players.Count; i += parallel)
            {
                await Task.WhenAll(players.Skip(i).Take(parallel).Select(async p =>
                {
                    var pInfo = await _siteParser
                        .GetPlayerInformationAsync(p.UrlName, Player.DefaultPlayerHexColor)
                        .ConfigureAwait(false);

                    if (pInfo != null)
                    {
                        pInfo.Id = p.Id;
                        okPlayers.Add(new Player(pInfo));
                    }
                })).ConfigureAwait(false);
            }

            return okPlayers;
        }

        public async Task ScanTimePageForNewPlayersAsync(DateTime? stopAt, bool addEntries)
        {
            var (validPlayers, bannedPlayers) = await GetPlayersAsync().ConfigureAwait(false);

            // Takes GoldenEye as default date (older than Perfect Dark)
            var allDatesToLoop = (stopAt ?? Game.GoldenEye.GetEliteFirstDate()).LoopBetweenDates(DateStep.Month, _clock).Reverse().ToList();

            var playersToCreate = new ConcurrentBag<string>();

            var allEntries = new ConcurrentBag<EntryWebDto>();

            const int parallel = 8;
            for (var i = 0; i < allDatesToLoop.Count; i += parallel)
            {
                await Task.WhenAll(allDatesToLoop.Skip(i).Take(parallel).Select(async loopDate =>
                {
                    var results = await _siteParser
                        .GetMonthPageTimeEntriesAsync(loopDate.Year, loopDate.Month)
                        .ConfigureAwait(false);

                    foreach (var entry in results)
                    {
                        if (!bannedPlayers.Any(p => p.UrlName.Equals(entry.PlayerUrlName, StringComparison.InvariantCultureIgnoreCase))
                            && !validPlayers.Any(p => p.UrlName.Equals(entry.PlayerUrlName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            playersToCreate.Add(entry.PlayerUrlName);
                        }
                    }

                    allEntries.AddRange(results);
                })).ConfigureAwait(false);
            }

            var newPlayers = new Dictionary<string, long>();
            foreach (var pName in playersToCreate.Distinct())
            {
                var id = await _writeRepository
                    .InsertPlayerAsync(pName, Player.DefaultPlayerHexColor)
                    .ConfigureAwait(false);
                newPlayers.Add(pName, id);
            }

            if (addEntries)
            {
                foreach (var entry in allEntries)
                {
                    var idMatch = validPlayers
                            .FirstOrDefault(p =>
                                p.UrlName.Equals(
                                    entry.PlayerUrlName,
                                    StringComparison.InvariantCultureIgnoreCase))?
                            .Id;

                    // should use InvariantCultureIgnoreCase
                    if (!idMatch.HasValue && newPlayers.ContainsKey(entry.PlayerUrlName))
                    {
                        idMatch = newPlayers[entry.PlayerUrlName];
                    }

                    if (idMatch.HasValue)
                    {
                        var dto = entry.ToEntry(idMatch.Value);
                        dto.Engine = await _siteParser
                            .GetTimeEntryEngineAsync(entry.EngineUrl)
                            .ConfigureAwait(false);

                        await _writeRepository
                            .InsertTimeEntryAsync(dto)
                            .ConfigureAwait(false);
                    }
                    else
                    {

                    }
                }
            }
        }

        public async Task<bool> CleanDirtyPlayerAsync(long playerId)
        {
            var players = await _readRepository
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            var p = players.SingleOrDefault(_ => _.Id == playerId);
            if (p == null)
                return false;

            var pInfo = await _siteParser
                .GetPlayerInformationAsync(p.UrlName, Player.DefaultPlayerHexColor)
                .ConfigureAwait(false);
            if (pInfo == null)
                return false;

            pInfo.Id = playerId;

            await _writeRepository
                .CleanPlayerAsync(pInfo)
                .ConfigureAwait(false);

            return true;
        }

        public async Task CheckPotentialBannedPlayersAsync()
        {
            var nonDirtyPlayers = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            const int parallel = 8;
            for (var i = 0; i < nonDirtyPlayers.Count; i += parallel)
            {
                await Task.WhenAll(nonDirtyPlayers.Skip(i).Take(parallel).Select(async p =>
                {
                    var res = await _siteParser
                        .GetPlayerInformationAsync(p.UrlName, Player.DefaultPlayerHexColor)
                        .ConfigureAwait(false);

                    if (res == null)
                    {
                        await _writeRepository
                            .UpdateDirtyPlayerAsync(p.Id)
                            .ConfigureAwait(false);
                    }
                })).ConfigureAwait(false);
            }
        }

        private async Task<(List<PlayerDto> validPlayers, List<PlayerDto> bannedPlayers)> GetPlayersAsync()
        {
            var players = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var dirtyPlayers = await _readRepository
                .GetBannedPlayersAsync()
                .ConfigureAwait(false);

            return (players.Concat(dirtyPlayers.Where(p => !p.IsBanned)).ToList(),
                dirtyPlayers.Where(p => p.IsBanned).ToList());
        }

        private async Task ExtractPlayerTimesAsync(Game game, PlayerDto player)
        {
            var entries = await _siteParser
                .GetPlayerEntriesAsync(game, player.UrlName)
                .ConfigureAwait(false);

            if (entries != null)
            {
                foreach (var stage in game.GetStages())
                {
                    await _writeRepository
                        .DeletePlayerStageEntriesAsync(stage, player.Id)
                        .ConfigureAwait(false);
                }

                var groupEntries = entries.GroupBy(e => (e.Stage, e.Level, e.Time, e.Engine));
                foreach (var group in groupEntries)
                {
                    var groupEntry = group.OrderBy(d => d.Date ?? DateTime.MaxValue).First();
                    await _writeRepository
                        .InsertTimeEntryAsync(groupEntry.ToEntry(player.Id))
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
