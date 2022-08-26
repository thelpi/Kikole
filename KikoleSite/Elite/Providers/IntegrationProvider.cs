using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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

        public async Task RefreshPlayersAsync()
        {
            var gePlayerUrls = await _siteParser
                .GetPlayerUrlsAsync(Game.GoldenEye)
                .ConfigureAwait(false);

            var pdPlayerUrls = await _siteParser
                .GetPlayerUrlsAsync(Game.PerfectDark)
                .ConfigureAwait(false);

            var allPlayerUrls = gePlayerUrls
                .Concat(pdPlayerUrls)
                .Distinct()
                .Select(pUrl => HttpUtility.UrlDecode(pUrl))
                .ToList();

            var (validPlayers, bannedPlayers) = await GetPlayersAsync()
                .ConfigureAwait(false);

            foreach (var pUrl in allPlayerUrls)
            {
                if (!validPlayers.Any(p => p.IsSame(pUrl)))
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
                    }
                }
            }

            var playersToBan = validPlayers
                .Where(p => !allPlayerUrls.Any(pUrl => p.IsSame(pUrl)))
                .ToList();

            foreach (var pToBan in playersToBan)
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

        private async Task<(IReadOnlyCollection<PlayerDto> validPlayers, IReadOnlyCollection<PlayerDto> bannedPlayers)> GetPlayersAsync()
        {
            var players = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var dirtyPlayers = await _readRepository
                .GetPlayersAsync(true)
                .ConfigureAwait(false);

            return (players, dirtyPlayers);
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
                        .InsertTimeEntryAsync(groupEntry.ToEntry(player.Id))
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
