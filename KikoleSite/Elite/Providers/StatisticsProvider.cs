using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Elite.Configurations;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;
using KikoleSite.Elite.Models;
using KikoleSite.Elite.Repositories;
using Microsoft.Extensions.Options;

namespace KikoleSite.Elite.Providers
{
    public sealed class StatisticsProvider : IStatisticsProvider
    {
        private readonly ICacheManager _cacheManager;
        private readonly IReadRepository _readRepository;
        private readonly RankingConfiguration _configuration;
        private readonly Api.Interfaces.IClock _clock;

        private static readonly TimeSpan StageLevelDefaultTime = TimeSpan.FromSeconds(RankingEntryLight.UnsetTimeValueSeconds);
        private static readonly TimeSpan FullGameDefaultTime = StageLevelDefaultTime.Multiply(20 * 3); // 3 levels, 20 stages

        public StatisticsProvider(
            IReadRepository readRepository,
            IOptions<RankingConfiguration> configuration,
            Api.Interfaces.IClock clock,
            ICacheManager cacheManager)
        {
            _readRepository = readRepository;
            _configuration = configuration.Value;
            _clock = clock;
            _cacheManager = cacheManager;
        }

        public async Task<IReadOnlyCollection<Standing>> GetLongestStandingsAsync(
            Game game,
            DateTime? endDate,
            StandingType standingType,
            bool? stillOngoing,
            Engine? engine,
            long? playerId,
            long? slayerPlayerId)
        {
            var standings = new List<Standing>();

            var wrs = await GetWorldRecordsAsync(game, endDate, engine).ConfigureAwait(false);

            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    var locWrs = wrs
                        .Where(wr => wr.Stage == stage && wr.Level == level)
                        .OrderBy(wr => wr.Date)
                        .ThenByDescending(wr => wr.Time);

                    Standing currentStanding = null;
                    foreach (var locWr in locWrs)
                    {
                        switch (standingType)
                        {
                            case StandingType.Unslayed:
                            case StandingType.FirstUnslayed:
                                standings.AddRange(
                                    locWr.Holders
                                        .Take(standingType == StandingType.FirstUnslayed
                                            ? 1
                                            : locWr.Holders.Count)
                                        .Select(_ => new Standing(locWr.Time)
                                        {
                                            Slayer = locWr.SlayPlayer,
                                            EndDate = locWr.SlayDate,
                                            StartDate = _.Item2,
                                            Author = _.Item1,
                                            Level = level,
                                            Stage = stage
                                        }));
                                break;
                            case StandingType.UnslayedExceptSelf:
                                var slayer = locWr.SlayPlayer;
                                var holders = locWr.Holders.ToList();
                                if (currentStanding != null)
                                {
                                    currentStanding.AddTime(locWr.Time);
                                    holders.RemoveAll(_ => _.Item1.Id == currentStanding.Author.Id);
                                    if (slayer == null || slayer.Id != currentStanding.Author.Id)
                                    {
                                        currentStanding.Slayer = slayer;
                                        currentStanding.EndDate = locWr.SlayDate;
                                        currentStanding = null;
                                    }
                                }
                                foreach (var holder in holders)
                                {
                                    var locCurrentStanding = new Standing(locWr.Time)
                                    {
                                        StartDate = holder.Item2,
                                        Author = holder.Item1,
                                        Level = level,
                                        Stage = stage
                                    };
                                    standings.Add(locCurrentStanding);

                                    if (slayer == null || slayer.Id != locCurrentStanding.Author.Id)
                                    {
                                        locCurrentStanding.Slayer = slayer;
                                        locCurrentStanding.EndDate = locWr.SlayDate;
                                    }
                                    else
                                    {
                                        currentStanding = locCurrentStanding;
                                    }
                                }
                                break;
                            case StandingType.Untied:
                                standings.Add(new Standing(locWr.Time)
                                {
                                    Slayer = locWr.UntiedSlayPlayer ?? locWr.SlayPlayer,
                                    EndDate = locWr.UntiedSlayDate ?? locWr.SlayDate,
                                    StartDate = locWr.Date,
                                    Author = locWr.Player,
                                    Level = level,
                                    Stage = stage
                                });
                                break;
                            case StandingType.UntiedExceptSelf:
                                if (currentStanding == null)
                                {
                                    currentStanding = new Standing(locWr.Time)
                                    {
                                        StartDate = locWr.Date,
                                        Author = locWr.Player,
                                        Level = level,
                                        Stage = stage
                                    };
                                    standings.Add(currentStanding);
                                }

                                currentStanding.AddTime(locWr.Time);

                                var untiedSlayer = locWr.UntiedSlayPlayer ?? locWr.SlayPlayer;
                                if (untiedSlayer == null || untiedSlayer.Id != currentStanding.Author.Id)
                                {
                                    currentStanding.Slayer = untiedSlayer;
                                    currentStanding.EndDate = locWr.UntiedSlayDate ?? locWr.SlayDate;
                                    currentStanding = null;
                                }
                                break;
                            case StandingType.BetweenTwoTimes:
                                for (var i = 0; i < locWr.Holders.Count; i++)
                                {
                                    var holder = locWr.Holders.ElementAt(i);
                                    var isLast = i == locWr.Holders.Count - 1;
                                    standings.Add(new Standing(locWr.Time)
                                    {
                                        Slayer = isLast
                                            ? locWr.SlayPlayer
                                            : locWr.Holders.ElementAt(i + 1).Item1,
                                        EndDate = isLast
                                            ? locWr.SlayDate
                                            : locWr.Holders.ElementAt(i + 1).Item2,
                                        StartDate = holder.Item2,
                                        Author = holder.Item1,
                                        Level = level,
                                        Stage = stage
                                    });
                                }
                                break;
                        }
                    }
                }
            }

            return standings
                .Where(x => (stillOngoing == true
                    ? !x.EndDate.HasValue
                    : (stillOngoing != false || x.EndDate.HasValue))
                    && (!playerId.HasValue || x.Author.Id == playerId)
                    && (!slayerPlayerId.HasValue || x.Slayer?.Id == slayerPlayerId))
                .OrderByDescending(x => x.WithDays(endDate ?? _clock.Now).Days)
                .ToList()
                .WithRanks(x => x.Days.Value);
        }

        public async Task<IReadOnlyCollection<Player>> GetPlayersAsync(
            bool useCache = false,
            string pattern = null)
        {
            var playersKeys = await GetPlayersInternalAsync(useCache: useCache).ConfigureAwait(false);

            var players = playersKeys.Select(kvp => kvp.Value);

            if (!string.IsNullOrWhiteSpace(pattern))
            {
                players = players.Where(p =>
                    p.RealName.Contains(pattern, StringComparison.InvariantCultureIgnoreCase)
                    || p.SurName.Contains(pattern, StringComparison.InvariantCultureIgnoreCase));
            }

            return players
                .Select(p => new Player(p))
                .OrderBy(p => p.Color)
                .ToList();
        }

        public async Task<IReadOnlyCollection<StageLeaderboard>> GetStageLeaderboardHistoryAsync(
            Stage stage,
            LeaderboardGroupOptions groupOption,
            int daysStep,
            long? playerId)
        {
            var players = await GetPlayersInternalAsync().ConfigureAwait(false);

            var entries = await GetStageEntriesCoreAsync(stage, players).ConfigureAwait(false);

            var leaderboards = new List<StageLeaderboard>(9125); // 25y * 365d
            var startDate = stage.GetGame().GetEliteFirstDate();
            foreach (var date in SystemExtensions.LoopBetweenDates(startDate, _clock.Tomorrow, DateStep.Day, daysStep))
            {
                if (date > startDate)
                {
                    var leaderboard = GetSpecificDateStageLeaderboard(stage, players, entries, startDate, date, playerId);
                    if (leaderboard != null)
                        leaderboards.Add(leaderboard);
                }
                startDate = date;
            }

            return ConsolidateLeaderboards(leaderboards, groupOption, playerId.HasValue);
        }

        public async Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(
            RankingRequest request)
        {
            request.Players = await GetPlayersInternalAsync(request.Country)
                .ConfigureAwait(false);

            return await GetFullGameConsolidatedRankingAsync(request)
                .ConfigureAwait(false);
        }

        public async Task<PlayerRankingHistory> GetPlayerRankingHistoryAsync(
            Game game,
            long playerId)
        {
            // gets all entries
            var entries = new Dictionary<(Stage s, Level l), List<EntryDto>>();
            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    var sourceEntries = await _cacheManager
                        .GetStageLevelEntriesAsync(stage, level)
                        .ConfigureAwait(false);
                    entries.Add((stage, level), sourceEntries.ToList());
                }
            }

            // finds if at least one for the player
            var entriesForMinDate = entries.SelectMany(x => x.Value).Where(x => x.PlayerId == playerId);
            if (!entriesForMinDate.Any())
                return new PlayerRankingHistory(new List<PlayerRankingLight>());

            // tries to fill blank dates
            foreach (var (s, l) in entries.Keys)
                ManageDateLessEntries(game, entries[(s, l)]);

            // represents all dates with at least one change, after the player has join
            var minDate = entriesForMinDate.Min(x => x.Date);
            var dates = entries
                .SelectMany(x => x.Value)
                .Select(x => x.Date.Value)
                .Distinct()
                .Where(x => x >= minDate)
                .OrderBy(x => x)
                .ToList();

            // represents all players ID
            var playersId = entries
                .SelectMany(x => x.Value)
                .Select(x => x.PlayerId)
                .Distinct()
                .ToList();

            // shortcut to get if time entry exits for a date
            var datesForStageLevel = entries
                .ToDictionary(x => x.Key, x => new HashSet<DateTime>(x.Value
                    .GroupBy(_ => _.Date.Value)
                    .Select(_ => _.Key)));

            // for each stage/level, the rankings are sorted by date asc
            var stageLevelRankings = new ConcurrentDictionary<(Stage s, Level l), IReadOnlyDictionary<DateTime, IReadOnlyDictionary<long, PlayerStageLevelRankingLight>>>();

            Parallel.ForEach(SystemExtensions.Enumerate<Level>(), level =>
            {
                foreach (var stage in game.GetStages())
                {
                    var stageLevelRanking = new Dictionary<DateTime, IReadOnlyDictionary<long, PlayerStageLevelRankingLight>>();
                    var isLoopFirstDate = true;
                    foreach (var date in dates)
                    {
                        if (!isLoopFirstDate)
                        {
                            if (!datesForStageLevel[(stage, level)].Contains(date))
                            {
                                stageLevelRanking.Add(date, stageLevelRanking.Values.Last());
                                continue;
                            }
                        }

                        var localRankings = new Dictionary<long, PlayerStageLevelRankingLight>();

                        // keeps the best time for each player
                        var entriesAtDate = entries[(stage, level)]
                            .Where(x => x.Date <= date)
                            .GroupBy(x => x.PlayerId)
                            .Select(x => x.OrderBy(x => x.Time).ThenBy(x => x.Date).First())
                            .OrderBy(x => x.Time)
                            .ToList();

                        // compute points
                        long? time = null;
                        int playersCountForTime = 1;
                        var points = StageLeaderboard.BasePoints;
                        foreach (var entry in entriesAtDate)
                        {
                            if (!time.HasValue || time != entry.Time)
                            {
                                if (time.HasValue)
                                {
                                    for (var i = 0; i < playersCountForTime; i++)
                                        points = StageLeaderboard.PointsChart.TryGetValue(points, out int tmpPoints) ? tmpPoints : points - 1;
                                }
                                playersCountForTime = 1;
                                time = entry.Time;
                            }
                            else
                                playersCountForTime++;

                            localRankings.Add(entry.PlayerId, new PlayerStageLevelRankingLight
                            {
                                Date = date,
                                Level = level,
                                PlayerId = entry.PlayerId,
                                Stage = stage,
                                Time = new TimeSpan(0, 0, (int)entry.Time),
                                Points = points
                            });
                        }

                        stageLevelRanking.Add(date, localRankings);
                        isLoopFirstDate = false;
                    }
                    stageLevelRankings.TryAdd((stage, level), stageLevelRanking);
                }
            });

            // for each date, creates a full ranking, then we get only the player ranking
            var chronologyRankings = new ConcurrentBag<PlayerRankingLight>();

            const int parallelism = 4;
            var datesCount = dates.Count % parallelism > 0
                ? (dates.Count / parallelism) + 1
                : dates.Count / parallelism;
            Parallel.For(0, parallelism, ip =>
            {
                foreach (var date in dates.Skip(ip * datesCount).Take(datesCount))
                {
                    // transforms each player into a ranking instance
                    PlayerRankingLight myRanking = null;
                    var rks = playersId
                        .Select(x =>
                        {
                            var localRk = new PlayerRankingLight
                            {
                                Date = date,
                                Game = game,
                                PlayerId = x,
                                Time = FullGameDefaultTime
                            };
                            if (x == playerId)
                                myRanking = localRk;
                            return localRk;
                        })
                        .ToList();

                    // adjusts each ranking with data from the tuple stage/level closest to the current date
                    foreach (var stage in game.GetStages())
                    {
                        foreach (var level in SystemExtensions.Enumerate<Level>())
                        {
                            var stageLevelRankingCurrent = stageLevelRankings[(stage, level)]
                                .LastOrDefault(x => x.Key.Date <= date);
                            foreach (var x in rks.Where(x => stageLevelRankingCurrent.Value?.ContainsKey(x.PlayerId) == true))
                            {
                                x.Points += stageLevelRankingCurrent.Value[x.PlayerId].Points;
                                x.Time = x.Time.Subtract(StageLevelDefaultTime - stageLevelRankingCurrent.Value[x.PlayerId].Time);
                            }
                        }
                    }

                    // sets the ranking by points
                    rks = rks
                        .OrderByDescending(x => x.Points)
                        .ToList();

                    rks.SetRank((x1, x2) => x1.Points == x2.Points, x => x.PointsRank, (x, r) => x.PointsRank = r);

                    // sets the ranking by time
                    rks = rks
                        .OrderBy(x => x.Time)
                        .ToList();

                    rks.SetRank((x1, x2) => x1.Time == x2.Time, x => x.TimeRank, (x, r) => x.TimeRank = r);

                    chronologyRankings.Add(myRanking);
                }
            });

            var chronologyRankingsList = chronologyRankings
                .OrderBy(x => x.Date)
                .ToList();

            return new PlayerRankingHistory(chronologyRankingsList
                .Where((x, i) => i == 0 || x.HasChanged(chronologyRankingsList[i - 1]))
                .ToList());
        }

        public async Task<(DateTime? firstDate, DateTime? lastDate)> GetPlayerActivityDatesAsync(
            Game game,
            long playerId)
        {
            var entries = await _cacheManager
                .GetPlayerEntriesAsync(game, playerId)
                .ConfigureAwait(false);

            return entries.Count == 0
                ? (null, null)
                : (entries.Min(_ => _.Date), entries.Max(_ => _.Date));
        }

        private async Task<List<RankingEntryLight>> GetFullGameConsolidatedRankingAsync(RankingRequest request)
        {
            // Gets ranking
            var finalEntries = await GetFullGameRankingAsync(request)
                .ConfigureAwait(false);

            var rankingEntries = finalEntries
                .GroupBy(e => e.PlayerId)
                .Select(e => request.FullDetails
                    ? new RankingEntry(request.Game, request.Players[e.Key])
                    : new RankingEntryLight(request.Game, request.Players[e.Key]))
                .ToList();

            foreach (var entryGroup in finalEntries.GroupBy(r => new { r.Stage, r.Level }))
            {
                foreach (var timesGroup in entryGroup.GroupBy(l => l.Time).OrderBy(l => l.Key))
                {
                    var rank = timesGroup.First().Rank;
                    bool isUntied = rank == 1 && timesGroup.Count() == 1;

                    foreach (var timeEntry in timesGroup)
                    {
                        rankingEntries
                            .Single(e => e.Player.Id == timeEntry.PlayerId)
                            .AddStageAndLevelDatas(timeEntry, isUntied);
                    }
                }
            }

            return rankingEntries
                .OrderByDescending(r => r.Points)
                .ToList()
                .WithRanks(r => r.Points);
        }

        private async Task<List<RankingDto>> GetFullGameRankingAsync(RankingRequest request)
        {
            var rankingEntries = new ConcurrentBag<RankingDto>();

            var tasks = new List<Task>();
            foreach (var stage in request.Game.GetStages())
            {
                tasks.Add(Task.Run(async () =>
                {
                    foreach (var level in SystemExtensions.Enumerate<Level>())
                    {
                        var stageLevelRankings = await GetStageLevelRankingAsync(request, stage, level)
                            .ConfigureAwait(false);
                        rankingEntries.AddRange(stageLevelRankings);
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return rankingEntries.ToList();
        }

        private async Task<List<RankingDto>> GetStageLevelRankingAsync(
            RankingRequest request,
            Stage stage,
            Level level)
        {
            var entries = await GetStageLevelEntriesAsync(request, stage, level)
                .ConfigureAwait(false);

            // Groups and sorts by date
            var entriesDateGroup = new SortedList<DateTime, List<EntryDto>>(
                entries
                    .GroupBy(e => e.Date.Value.Date)
                    .ToDictionary(
                        eGroup => eGroup.Key,
                        eGroup => eGroup.ToList()));

            var rankingsToInsert = new List<RankingDto>();

            // For the current date + previous days
            // Gets the min time entry for each player
            // Then orders by entry time overall (ascending)
            var selectedEntries = entriesDateGroup
                .Where(kvp => kvp.Key <= request.RankingDate)
                .SelectMany(kvp => kvp.Value)
                .GroupBy(e => e.PlayerId)
                .Select(eGroup => eGroup.First(e => e.Time == eGroup.Min(et => et.Time)))
                .OrderBy(e => e.Time)
                .ThenBy(e => e.Date.Value)
                .ToList();

            var pos = 1;
            var posAgg = 1;
            long? currentTime = null;
            foreach (var entry in selectedEntries)
            {
                if (!currentTime.HasValue)
                {
                    currentTime = entry.Time;
                }
                else if (currentTime == entry.Time)
                {
                    posAgg++;
                }
                else
                {
                    pos += posAgg;
                    posAgg = 1;
                    currentTime = entry.Time;
                }

                var ranking = new RankingDto
                {
                    Date = request.RankingDate,
                    Level = entry.Level,
                    PlayerId = entry.PlayerId,
                    Rank = pos,
                    Stage = entry.Stage,
                    Time = entry.Time,
                    EntryDate = entry.Date.Value,
                    IsSimulatedDate = entry.IsSimulatedDate
                };

                rankingsToInsert.Add(ranking);
            }

            return rankingsToInsert;
        }

        private async Task<List<EntryDto>> GetStageLevelEntriesAsync(
            RankingRequest request,
            Stage stage,
            Level level)
        {
            var entries = await GetStageLevelEntriesCoreAsync(
                    request.Players, stage, level, request.CountryPlayersGroup)
                .ConfigureAwait(false);

            if (request.Engine.HasValue)
            {
                entries.RemoveAll(_ => (_.Engine != Engine.UNK && _.Engine != request.Engine.Value)
                    || (!request.IncludeUnknownEngine && _.Engine == Engine.UNK));
            }

            if (request.RankingStartDate.HasValue)
            {
                entries.RemoveAll(_ => _.Date < request.RankingStartDate.Value);
            }

            if (request.PlayerVsLegacy.HasValue)
            {
                entries.RemoveAll(_ => _.Date > request.PlayerVsLegacy.Value.Item2
                    && _.PlayerId != request.PlayerVsLegacy.Value.Item1);
            }

            return entries;
        }

        private async Task<List<EntryDto>> GetStageLevelEntriesCoreAsync(
            IReadOnlyDictionary<long, PlayerDto> players,
            Stage stage,
            Level level,
            IReadOnlyDictionary<long, IReadOnlyCollection<long>> countryPlayersGroup = null)
        {
            // Gets every entry for the stage and level
            var tmpEntriesSource = await _cacheManager
                .GetStageLevelEntriesAsync(stage, level)
                .ConfigureAwait(false);

            // if country grouping, replace player by "country player"
            if (countryPlayersGroup?.Count > 0)
            {
                foreach (var entry in tmpEntriesSource)
                {
                    entry.PlayerId = countryPlayersGroup.Single(_ => _.Value.Contains(entry.PlayerId)).Key;
                }
            }

            // Entries not related to players are excluded
            var entries = tmpEntriesSource
                .Where(e => players.ContainsKey(e.PlayerId))
                .ToList();

            // Sets date for every entry
            ManageDateLessEntries(stage.GetGame(), entries);

            return entries;
        }

        private async Task<IReadOnlyDictionary<long, PlayerDto>> GetPlayersInternalAsync(string country = null, bool useCache = false)
        {
            var playersList = await _readRepository
                .GetPlayersAsync(banned: false, fromCache: useCache)
                .ConfigureAwait(false);

            return playersList
                .Where(p => string.IsNullOrWhiteSpace(country)
                    || country.Equals(p.Country, StringComparison.InvariantCultureIgnoreCase))
                .ToDictionary(p => p.Id, p => p);
        }

        // Sets a fake date on entries without it
        private void ManageDateLessEntries(
            Game game,
            List<EntryDto> entries)
        {
            if (_configuration.NoDateEntryRankingRule == NoDateEntryRankingRule.Ignore)
            {
                entries.RemoveAll(e => !e.Date.HasValue);
            }
            else
            {
                var dateMinMaxPlayer = new Dictionary<long, (DateTime Min, DateTime Max, IReadOnlyCollection<EntryDto> Entries)>();

                var dateLessEntries = entries.Where(e => !e.Date.HasValue).ToList();
                foreach (var entry in dateLessEntries)
                {
                    if (!dateMinMaxPlayer.ContainsKey(entry.PlayerId))
                    {
                        var dateMin = entries
                            .Where(e => e.PlayerId == entry.PlayerId && e.Date.HasValue)
                            .Select(e => e.Date.Value)
                            .Concat(game.GetEliteFirstDate().Yield())
                            .Min();
                        var dateMax = entries.Where(e => e.PlayerId == entry.PlayerId).Max(e => e.Date ?? Player.LastEmptyDate);
                        dateMinMaxPlayer.Add(entry.PlayerId, (dateMin, dateMax, entries.Where(e => e.PlayerId == entry.PlayerId).ToList()));
                    }

                    // Same time with a known date (possible for another engine/system)
                    var sameEntry = dateMinMaxPlayer[entry.PlayerId].Entries.FirstOrDefault(e => e.Stage == entry.Stage && e.Level == entry.Level && e.Time == entry.Time && e.Date.HasValue);
                    // Better time (closest to current) with a known date
                    var betterEntry = dateMinMaxPlayer[entry.PlayerId].Entries.OrderBy(e => e.Time).FirstOrDefault(e => e.Stage == entry.Stage && e.Level == entry.Level && e.Time < entry.Time && e.Date.HasValue);
                    // Worse time (closest to current) with a known date
                    var worseEntry = dateMinMaxPlayer[entry.PlayerId].Entries.OrderByDescending(e => e.Time).FirstOrDefault(e => e.Stage == entry.Stage && e.Level == entry.Level && e.Time < entry.Time && e.Date.HasValue);

                    if (sameEntry != null)
                    {
                        // use the another engine/system date as the current date
                        entry.Date = sameEntry.Date;
                        entry.IsSimulatedDate = true;
                    }
                    else
                    {
                        var realMin = dateMinMaxPlayer[entry.PlayerId].Min;
                        if (worseEntry != null && worseEntry.Date > realMin)
                        {
                            realMin = worseEntry.Date.Value;
                        }

                        var realMax = dateMinMaxPlayer[entry.PlayerId].Max;
                        if (betterEntry != null && betterEntry.Date < realMax)
                        {
                            realMax = betterEntry.Date.Value;
                        }

                        // when the min / max theoric is too wide to set a proper date
                        if (realMin == game.GetEliteFirstDate() && realMax == Player.LastEmptyDate)
                        {
                            entries.Remove(entry);
                            continue;
                        }

                        switch (_configuration.NoDateEntryRankingRule)
                        {
                            case NoDateEntryRankingRule.Average:
                                entry.Date = realMin.AddDays((realMax - realMin).TotalDays / 2).Date;
                                entry.IsSimulatedDate = true;
                                break;
                            case NoDateEntryRankingRule.Max:
                                entry.Date = realMax;
                                entry.IsSimulatedDate = true;
                                break;
                            case NoDateEntryRankingRule.Min:
                                entry.Date = realMin;
                                entry.IsSimulatedDate = true;
                                break;
                            case NoDateEntryRankingRule.PlayerHabit:
                                var entriesBetween = dateMinMaxPlayer[entry.PlayerId].Entries
                                    .Where(e => e.Date < realMax && e.Date > realMin)
                                    .Select(e => Convert.ToInt32((_clock.Now - e.Date.Value).TotalDays))
                                    .ToList();
                                if (entriesBetween.Count == 0)
                                {
                                    entry.Date = realMin.AddDays((realMax - realMin).TotalDays / 2).Date;
                                }
                                else
                                {
                                    var avgDays = entriesBetween.Average();
                                    entry.Date = _clock.Now.AddDays(-avgDays).Date;
                                }
                                entry.IsSimulatedDate = true;
                                break;
                        }
                    }
                }
            }
        }

        private IReadOnlyCollection<Wr> GetStageLevelWorldRecords(
            IReadOnlyCollection<EntryDto> allEntries,
            IReadOnlyDictionary<long, PlayerDto> players,
            Stage stage,
            Level level,
            DateTime? endDate,
            Engine? engine)
        {
            endDate = (endDate ?? _clock.Now).Date;

            var wrs = new List<Wr>();

            var entries = allEntries
                .Where(e => e.Date <= endDate && (!engine.HasValue || engine == e.Engine))
                .GroupBy(e => e.Date.Value)
                .OrderBy(e => e.Key)
                .ToDictionary(eg => eg.Key, eg => eg.OrderByDescending(e => e.Time).ToList());

            var bestTime = entries[entries.Keys.First()].Select(_ => _.Time).Min();
            Wr currentWr = null;
            foreach (var entryDate in entries.Keys)
            {
                var betterOrEqualEntries = entries[entryDate].Where(e => e.Time <= bestTime);
                foreach (var entry in betterOrEqualEntries)
                {
                    var player = players[entry.PlayerId];

                    if (entry.Time == bestTime && currentWr != null)
                        currentWr.AddHolder(player, entryDate, entry.Engine);
                    else
                    {
                        currentWr?.AddSlayer(player, entryDate);

                        currentWr = new Wr(stage, level, entry.Time, player, entryDate, entry.Engine);
                        wrs.Add(currentWr);
                        bestTime = entry.Time;
                    }
                }
            }

            return wrs;
        }

        private async Task<IReadOnlyCollection<Wr>> GetWorldRecordsAsync(
            Game game,
            DateTime? endDate,
            Engine? engine)
        {
            var wrs = new ConcurrentBag<Wr>();

            var players = await GetPlayersInternalAsync().ConfigureAwait(false);

            var tasks = new List<Task>();

            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var entries = await GetStageLevelEntriesCoreAsync(players, stage, level).ConfigureAwait(false);

                        wrs.AddRange(
                            GetStageLevelWorldRecords(entries, players, stage, level, endDate, engine));
                    }));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return wrs;
        }

        private static IReadOnlyCollection<StageLeaderboard> ConsolidateLeaderboards(
            List<StageLeaderboard> leaderboards,
            LeaderboardGroupOptions groupOption,
            bool singlePlayer)
        {
            var consolidedLeaderboards = new List<StageLeaderboard>(leaderboards.Count);
            if (StageLeaderboardItem.ComputeGroupOtions.ContainsKey(groupOption) && leaderboards.Count > 0)
            {
                var compareFunc = StageLeaderboardItem.ComputeGroupOtions[groupOption];

                IEqualityComparer<StageLeaderboardItem> comparer = EqualityComparer<StageLeaderboardItem>.Default;
                if (groupOption == LeaderboardGroupOptions.FirstRankedFirst)
                    comparer = new StageLeaderboardItemSamePlayer();
                else if (singlePlayer)
                {
                    if (groupOption == LeaderboardGroupOptions.Ranked)
                        comparer = new StageLeaderboardItemSameTier();
                    else if (groupOption == LeaderboardGroupOptions.RankedFirst
                        || groupOption == LeaderboardGroupOptions.RankedTop10)
                        comparer = new StageLeaderboardItemSameRank();
                }

                consolidedLeaderboards.Add(leaderboards[0]);
                for (var i = 1; i < leaderboards.Count; i++)
                {
                    if (compareFunc(leaderboards[i - 1].Items).SequenceEqual(compareFunc(leaderboards[i].Items), comparer))
                        consolidedLeaderboards.Last().DateEnd = leaderboards[i].DateEnd;
                    else
                        consolidedLeaderboards.Add(leaderboards[i]);
                }
            }
            else
                consolidedLeaderboards = leaderboards;

            return consolidedLeaderboards;
        }

        private static StageLeaderboard GetSpecificDateStageLeaderboard(
            Stage stage,
            IReadOnlyDictionary<long, PlayerDto> players,
            List<EntryDto> entries,
            DateTime startDate,
            DateTime endDate,
            long? playerId)
        {
            var dateEntries = entries
                .Where(_ => _.Date < endDate)
                .ToList();

            var playersPoints = new Dictionary<long, (int points, DateTime latestDate)>();

            // adds the player to the leaderboard, or update points and date for the player
            void AddOrUpdate(int entryPoints, EntryDto entry)
            {
                if (!playersPoints.ContainsKey(entry.PlayerId))
                {
                    if (entryPoints > 0)
                        playersPoints.Add(entry.PlayerId, (entryPoints, entry.Date.Value));
                }
                else
                {
                    var (points, latestDate) = playersPoints[entry.PlayerId];
                    playersPoints[entry.PlayerId] = (points + entryPoints, latestDate.Latest(entry.Date.Value));
                }
            }

            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                // for one level, gets the best time of each player
                var bestByPlayer = dateEntries
                    .Where(_ => _.Level == level)
                    .GroupBy(_ => _.PlayerId)
                    .Select(_ => _.OrderBy(e => e.Time).ThenBy(e => e.Date).First())
                    .OrderBy(_ => _.Time)
                    .ThenBy(_ => _.Date)
                    .ToList();

                long? time = null;
                int playersCountForTime = 1;
                var points = StageLeaderboard.BasePoints;
                foreach (var rec in bestByPlayer)
                {
                    if (!time.HasValue || time != rec.Time)
                    {
                        if (time.HasValue)
                        {
                            for (var i = 0; i < playersCountForTime; i++)
                                points = StageLeaderboard.PointsChart.TryGetValue(points, out int tmpPoints) ? tmpPoints : points - 1;
                        }
                        AddOrUpdate(points, rec);
                        playersCountForTime = 1;
                        time = rec.Time;
                    }
                    else
                    {
                        AddOrUpdate(points, rec);
                        playersCountForTime++;
                    }
                }
            }

            if (playerId.HasValue && !playersPoints.ContainsKey(playerId.Value))
                return null;

            var items = playersPoints
                .Select(_ => new StageLeaderboardItem
                {
                    LatestTime = _.Value.latestDate,
                    Player = new Player(players[_.Key]),
                    Points = _.Value.points
                })
                .OrderByDescending(_ => _.Points)
                .ThenBy(_ => _.LatestTime)
                .ToList();

            items.SetRank((r1, r2) => r1.Points == r2.Points, r => r.Rank, (r, i) => r.Rank = i);

            return new StageLeaderboard
            {
                DateEnd = endDate,
                DateStart = startDate,
                Items = items.Where(_ => !playerId.HasValue || _.Player.Id == playerId).ToList(),
                Stage = stage
            };
        }

        private async Task<List<EntryDto>> GetStageEntriesCoreAsync(Stage stage, IReadOnlyDictionary<long, PlayerDto> players)
        {
            var easyEntries = await GetStageLevelEntriesCoreAsync(players, stage, Level.Easy).ConfigureAwait(false);
            var mediumEntries = await GetStageLevelEntriesCoreAsync(players, stage, Level.Medium).ConfigureAwait(false);
            var hardEntries = await GetStageLevelEntriesCoreAsync(players, stage, Level.Hard).ConfigureAwait(false);
            return easyEntries.Concat(mediumEntries).Concat(hardEntries).ToList();
        }
    }
}
