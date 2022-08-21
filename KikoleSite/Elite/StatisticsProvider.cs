using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace KikoleSite.Elite
{
    public sealed class StatisticsProvider : IStatisticsProvider
    {
        private readonly IReadRepository _readRepository;
        private readonly RankingConfiguration _configuration;
        private readonly Api.Interfaces.IClock _clock;

        public StatisticsProvider(
            IReadRepository readRepository,
            IOptions<RankingConfiguration> configuration,
            Api.Interfaces.IClock clock)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            _clock = clock;
        }

        public async Task<IReadOnlyCollection<Standing>> GetLongestStandingsAsync(
            Game game,
            DateTime? endDate,
            StandingType standingType,
            bool? stillOngoing,
            Engine? engine)
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
                                standings.AddRange(locWr.Holders.Select(_ => new Standing(locWr.Time)
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

            var now = _clock.Now;

            standings = standings
                .Where(x => stillOngoing == true
                    ? !x.EndDate.HasValue
                    : (stillOngoing != false || x.EndDate.HasValue))
                .OrderByDescending(x => x.WithDays(now).Days)
                .ToList();

            if (standingType == StandingType.FirstUnslayed)
            {
                var tmpStandings = new List<Standing>(standings.Count);
                foreach (var std in standings)
                {
                    if (!tmpStandings.Any(_ => _.Stage == std.Stage
                        && _.Level == std.Level
                        && _.Times.Single() == std.Times.Single()))
                    {
                        tmpStandings.Add(std);
                    }
                }
                standings = tmpStandings;
            }

            return standings;
        }

        public async Task<IReadOnlyCollection<Player>> GetPlayersAsync()
        {
            var players = await GetPlayersInternalAsync().ConfigureAwait(false);

            return players
                .Select(p => new Player(p.Value))
                .ToList();
        }

        public async Task<IReadOnlyCollection<StageLeaderboard>> GetStageLeaderboardHistoryAsync(Stage stage, LeaderboardGroupOptions groupOption, int daysStep)
        {
            var players = await GetPlayersInternalAsync().ConfigureAwait(false);

            var entries = await GetStageEntriesCoreAsync(stage, players).ConfigureAwait(false);

            var leaderboards = new List<StageLeaderboard>(9125); // 25y * 365d
            var startDate = stage.GetGame().GetEliteFirstDate();
            foreach (var date in SystemExtensions.LoopBetweenDates(startDate, _clock.Tomorrow, DateStep.Day, daysStep))
            {
                if (date > startDate)
                {
                    var leaderboard = GetSpecificDateStageLeaderboard(stage, players, entries, startDate, date);

                    leaderboards.Add(leaderboard);
                }
                startDate = date;
            }

            return ConsolidateLeaderboards(leaderboards, groupOption);
        }

        private async Task<List<EntryDto>> GetStageLevelEntriesCoreAsync(
            IReadOnlyDictionary<long, PlayerDto> players,
            Stage stage,
            Level level,
            ConcurrentDictionary<(Stage, Level), IReadOnlyCollection<EntryDto>> entriesCache = null)
        {
            // Gets every entry for the stage and level
            var tmpEntriesSource = entriesCache?.ContainsKey((stage, level)) == true
                ? entriesCache[(stage, level)]
                : await _readRepository
                    .GetEntriesAsync(stage, level, null, null)
                    .ConfigureAwait(false);

            // Entries not related to players are excluded
            var entries = tmpEntriesSource
                .Where(e => players.ContainsKey(e.PlayerId))
                .ToList();

            // Sets date for every entry
            ManageDateLessEntries(stage.GetGame(), entries);

            if (entriesCache?.ContainsKey((stage, level)) == false)
            {
                entriesCache.TryAdd((stage, level), new List<EntryDto>(entries));
            }

            return entries;
        }

        // Gets a dictionary of every player by identifier (including dirty, but not banned)
        private async Task<IReadOnlyDictionary<long, PlayerDto>> GetPlayersInternalAsync()
        {
            var playersSourceClean = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var playersSourceDirty = await _readRepository
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            return playersSourceClean.Concat(playersSourceDirty).ToDictionary(p => p.Id, p => p);
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

        private static IEnumerable<(long, DateTime, Stage)> GetPotentialSweeps(
            bool untied,
            Dictionary<(Stage, Level), List<EntryDto>> entriesGroups,
            DateTime currentDate,
            Stage stage)
        {
            var tiedSweepPlayerIds = new List<long>();
            long? untiedSweepPlayerId = null;
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var stageLevelDateWrs = entriesGroups[(stage, level)]
                    .Where(e => e.Date.Value.Date <= currentDate.Date)
                    .GroupBy(e => e.Time)
                    .OrderBy(e => e.Key)
                    .FirstOrDefault();

                bool isPotentialSweep = untied
                    ? stageLevelDateWrs?.Count() == 1
                    : stageLevelDateWrs?.Count() > 0;

                if (isPotentialSweep)
                {
                    if (untied)
                    {
                        var currentPId = stageLevelDateWrs.First().PlayerId;
                        if (!untiedSweepPlayerId.HasValue)
                        {
                            untiedSweepPlayerId = currentPId;
                        }

                        isPotentialSweep = untiedSweepPlayerId.Value == currentPId;
                    }
                    else
                    {
                        tiedSweepPlayerIds = tiedSweepPlayerIds.IntersectOrConcat(stageLevelDateWrs.Select(_ => _.PlayerId));

                        isPotentialSweep = tiedSweepPlayerIds.Count > 0;
                    }
                }

                if (!isPotentialSweep)
                {
                    tiedSweepPlayerIds.Clear();
                    untiedSweepPlayerId = null;
                    break;
                }
            }

            if (!untied)
            {
                return tiedSweepPlayerIds.Select(_ => (_, currentDate, stage));
            }

            return untiedSweepPlayerId.HasValue
                ? (untiedSweepPlayerId.Value, currentDate.Date, stage).Yield()
                : Enumerable.Empty<(long, DateTime, Stage)>();
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

        private static IReadOnlyCollection<StageLeaderboard> ConsolidateLeaderboards(List<StageLeaderboard> leaderboards, LeaderboardGroupOptions groupOption)
        {
            var consolidedLeaderboards = new List<StageLeaderboard>(leaderboards.Count);
            if (StageLeaderboardItem.ComputeGroupOtions.ContainsKey(groupOption) && leaderboards.Count > 0)
            {
                var compareFunc = StageLeaderboardItem.ComputeGroupOtions[groupOption];

                IEqualityComparer<StageLeaderboardItem> comparer = EqualityComparer<StageLeaderboardItem>.Default;
                if (groupOption == LeaderboardGroupOptions.FirstRankedFirst)
                    comparer = new StageLeaderboardItemSamePlayer();

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

        private static StageLeaderboard GetSpecificDateStageLeaderboard(Stage stage, IReadOnlyDictionary<long, PlayerDto> players, List<EntryDto> entries, DateTime startDate, DateTime endDate)
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
                Items = items,
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
