using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;
using KikoleSite.Elite.Models;
using KikoleSite.Elite.Providers;
using KikoleSite.Elite.ViewDatas;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Elite.Controllers
{
    [Route("the-elite")]
    public class SimulatedRankingController : Controller
    {
        private const int MaxRankDisplay = 500;
        private const int DefaultLeaderboardDayStep = 7;
        private const int MaxStageParallelism = 4; // divisor of 20
        private const string AnonymiseColorRgb = "FFFFFF";

        private const string StageImagePath = @"/images/elite/{0}.jpg";
        private const string RankingViewName = "SimulatedRanking";
        private const string PlayersViewName = "Players";
        private const string PlayerDetailsViewName = "PlayerDetails";
        private const string IndexViewName = "Index";
        private const string ChronologyCanvasViewName = "ChronologyCanvas";
        private const string LongestStandingViewName = "LongestStanding";

        private readonly IStatisticsProvider _statisticsProvider;
        private readonly Api.Interfaces.IClock _clock;

        public SimulatedRankingController(
            IStatisticsProvider statisticsProvider,
            Api.Interfaces.IClock clock)
        {
            _statisticsProvider = statisticsProvider;
            _clock = clock;
        }

        [HttpGet("games/{game}/longest-standings")]
        public async Task<IActionResult> GetLongestStandingAsync(
            [FromRoute] Game game,
            [FromQuery] bool? stillOngoing,
            [FromQuery] long? playerId,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] StandingType standingType,
            [FromQuery] Engine? engine)
        {
            if (!CheckGameParameter(game))
            {
                return Json(new { error = "Invalid game value." });
            }

            if (!CheckEngineParameter(engine))
            {
                return Json(new { error = "The engine is invalid." });
            }

            if (!CheckStandingTypeParameter(standingType))
            {
                return Json(new { error = "The standing type is invalid." });
            }

            Player p = null;
            if (playerId.HasValue)
            {
                if (playerId <= 0)
                {
                    return Json(new { error = "Invalid player identifier." });
                }

                var players = await _statisticsProvider
                    .GetPlayersAsync()
                    .ConfigureAwait(false);
                p = players.SingleOrDefault(p => p.Id == playerId);
                if (p == null)
                {
                    return Json(new { error = "The player associated to the identifier does not exist." });
                }
            }

            var title = $"{game} - {standingType.GetStandingTypeDescription()}";
            if (engine.HasValue)
                title += $" - {engine} only";
            if (p != null)
                title += $" - {p.ToString(game)}";
            if (rankingDate.HasValue)
                title += $" - {rankingDate.Value:yyyy-MM-dd}";
            if (stillOngoing == true)
                title += " - Only ongoing entries";
            else if (stillOngoing == false)
                title += " - Only finished entries";

            return await ViewAsync(
                    LongestStandingViewName,
                    title,
                    async () =>
                    {
                        var results = await _statisticsProvider
                            .GetLongestStandingsAsync(game, rankingDate, standingType, stillOngoing, engine, playerId)
                            .ConfigureAwait(false);

                        return new LongestStandingViewData
                        {
                            Standings = results
                                .Select(_ => _.ToStandingItemData())
                                .ToList()
                        };
                    })
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/chronology-types/{chronologyType}/data")]
        public async Task<JsonResult> GetChronologyDataAsync(
            [FromRoute] Game game,
            [FromRoute] ChronologyTypeItemData chronologyType,
            [FromQuery] Engine? engine,
            [FromQuery] long? playerId,
            [FromQuery] byte anonymise)
        {
            if (!CheckGameParameter(game))
            {
                return Json(new { error = "Invalid game value." });
            }

            if (!CheckEngineParameter(engine))
            {
                return Json(new { error = "The engine is invalid." });
            }

            if (!CheckChronologyTypeParameter(chronologyType))
            {
                return Json(new { error = "The chronology type is invalid." });
            }

            if (playerId.HasValue)
            {
                if (playerId <= 0)
                {
                    return Json(new { error = "Invalid player identifier." });
                }

                var players = await _statisticsProvider
                    .GetPlayersAsync()
                    .ConfigureAwait(false);
                if (!players.Any(p => p.Id == playerId))
                {
                    return Json(new { error = "The player associated to the identifier does not exist." });
                }
            }
            else if (chronologyType.IsFullStage())
            {
                return Json(new { error = "Player identifier is mandatory for this type." });
            }

            List<ChronologyCanvasItemData> results;

            if (chronologyType.IsFullStage())
            {
                var itemGroups = new ConcurrentBag<IReadOnlyCollection<StageLeaderboard>>();
                var stages = game.GetStages();
                var tasks = new List<Task>(MaxStageParallelism);
                var stagesByTask = stages.Count / MaxStageParallelism;

                for (var i = 0; i < MaxStageParallelism; i++)
                {
                    var stagesGroup = stages.Skip(stagesByTask * i).Take(stagesByTask);
                    tasks.Add(Task.Run(async () =>
                    {
                        foreach (var stage in stagesGroup)
                        {
                            var stageItems = await _statisticsProvider
                                .GetStageLeaderboardHistoryAsync(stage, chronologyType.ToLeaderboardGroupOption(), DefaultLeaderboardDayStep, playerId)
                                .ConfigureAwait(false);

                            itemGroups.Add(stageItems);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                results = new List<ChronologyCanvasItemData>(5000); // arbitrary
                foreach (var itemGroup in itemGroups)
                {
                    foreach (var item in itemGroup)
                    {
                        results.AddRange(item.Items
                            .Select(_ => _.ToChronologyCanvasItemData(item, chronologyType, anonymise != 0, AnonymiseColorRgb)));
                    }
                }
            }
            else
            {
                var standings = await _statisticsProvider
                    .GetLongestStandingsAsync(game, null, chronologyType.ToStandingType(playerId.HasValue).Value, null, engine, playerId)
                    .ConfigureAwait(false);

                results = standings
                    .Select(_ => _.ToChronologyCanvasItemData(anonymise != 0, AnonymiseColorRgb))
                    .ToList();
            }

            return Json(results);
        }

        [HttpGet("games/{game}/wr-chronology")]
        public async Task<IActionResult> GetWorldRecordsChronologyAsync(
            [FromRoute] Game game,
            [FromQuery] ChronologyTypeItemData type,
            [FromQuery] Engine? engine,
            [FromQuery] long? playerId,
            [FromQuery] bool anonymise)
        {
            if (!CheckGameParameter(game))
            {
                return Json(new { error = "Invalid game value." });
            }

            if (!CheckEngineParameter(engine))
            {
                return Json(new { error = "The engine is invalid." });
            }

            if (!CheckChronologyTypeParameter(type) || type.IsFullStage())
            {
                return Json(new { error = "The chronology type is invalid." });
            }

            if (playerId.HasValue)
            {
                if (playerId <= 0)
                {
                    return Json(new { error = "Invalid player identifier." });
                }

                var players = await _statisticsProvider
                    .GetPlayersAsync()
                    .ConfigureAwait(false);
                if (!players.Any(p => p.Id == playerId))
                {
                    return Json(new { error = "The player associated to the identifier does not exist." });
                }
            }

            var model = new ChronologyCanvasViewData
            {
                TotalDays = (int)Math.Floor((_clock.Tomorrow - game.GetEliteFirstDate()).TotalDays),
                ChronologyType = type,
                Engine = engine,
                Game = game,
                PlayerId = playerId,
                Anonymise = anonymise,
                StageImages = game.GetStages().ToDictionary(_ => _, _ => string.Format(StageImagePath, (int)_))
            };

            return View($"Elite/Views/{ChronologyCanvasViewName}.cshtml", model);
        }

        [HttpGet("games/{game}/rk-chronology")]
        public async Task<IActionResult> GetRankingsChronologyAsync(
            [FromRoute] Game game,
            [FromQuery] ChronologyTypeItemData type,
            [FromQuery] long playerId,
            [FromQuery] bool anonymise)
        {
            if (!CheckGameParameter(game))
            {
                return Json(new { error = "Invalid game value." });
            }

            if (!CheckChronologyTypeParameter(type) || !type.IsFullStage())
            {
                return Json(new { error = "The chronology type is invalid." });
            }

            if (playerId <= 0)
            {
                return Json(new { error = "Invalid player identifier." });
            }

            var players = await _statisticsProvider
                .GetPlayersAsync()
                .ConfigureAwait(false);
            if (!players.Any(p => p.Id == playerId))
            {
                return Json(new { error = "The player associated to the identifier does not exist." });
            }

            var model = new ChronologyCanvasViewData
            {
                TotalDays = (int)Math.Floor((_clock.Tomorrow - game.GetEliteFirstDate()).TotalDays),
                ChronologyType = type,
                Game = game,
                PlayerId = playerId,
                Anonymise = anonymise,
                StageImages = game.GetStages().ToDictionary(_ => _, _ => string.Format(StageImagePath, (int)_))
            };

            return View($"Elite/Views/{ChronologyCanvasViewName}.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync()
        {
            return await ViewAsync(
                    IndexViewName,
                    "The Elite infographics - How to",
                    () => Task.FromResult((object)new IndexViewData()))
                .ConfigureAwait(false);
        }

        [HttpGet("players")]
        public async Task<IActionResult> GetPlayersAsync()
        {
            return await ViewAsync(
                PlayersViewName,
                "Players list",
                async () =>
                {
                    var players = await _statisticsProvider
                        .GetPlayersAsync()
                        .ConfigureAwait(false);

                    return players
                        .Select(p => p.ToPlayerItemData())
                        .ToList();
                }).ConfigureAwait(false);
        }

        [HttpGet("games/{game}/player-rankings")]
        public async Task<IActionResult> GetRankingByPlayerAsync(
            [FromRoute] Game game,
            [FromQuery] long playerId,
            [FromQuery] DateTime? rankingDate)
        {
            if (!CheckGameParameter(game))
            {
                return Json(new { error = "Invalid game value." });
            }

            if (playerId <= 0)
            {
                return Json(new { error = "Invalid player identifier." });
            }

            var players = await _statisticsProvider
                .GetPlayersAsync()
                .ConfigureAwait(false);
            if (!players.Any(p => p.Id == playerId))
            {
                return Json(new { error = "The player associated to the identifier does not exist." });
            }

            return await SimulateRankingInternalAsync(
                    game, rankingDate, playerId)
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/time-frame-rankings")]
        public async Task<IActionResult> GetRankingByTimeFrameAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [Required][FromQuery] DateTime rankingStartDate)
        {
            if (!CheckGameParameter(game))
            {
                return Json(new { error = "Invalid game value." });
            }

            if (rankingStartDate >= _clock.Tomorrow
                || (rankingDate.HasValue && rankingDate < rankingStartDate))
            {
                return Json(new { error = "The ranking start date is invalid." });
            }

            return await SimulateRankingInternalAsync(
                    game, rankingDate, rankingStartDate: rankingStartDate)
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/engine-rankings")]
        public async Task<IActionResult> GetRankingByEngineAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] Engine engine)
        {
            if (!CheckGameParameter(game))
            {
                return Json(new { error = "Invalid game value." });
            }

            if (!CheckEngineParameter(engine))
            {
                return Json(new { error = "The engine is invalid." });
            }

            return await SimulateRankingInternalAsync(
                    game, rankingDate, engine: engine)
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/country-rankings")]
        public async Task<IActionResult> GetRankingByCountryAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] string country)
        {
            if (!CheckGameParameter(game))
            {
                return Json(new { error = "Invalid game value." });
            }

            return await SimulateRankingInternalAsync(
                    game, rankingDate, country: country, countryGrouping: string.IsNullOrWhiteSpace(country))
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/players/{playerId}/details")]
        public async Task<IActionResult> GetPlayerDetailsForSpecifiedRankingAsync(
            [FromRoute] Game game,
            [FromRoute] long playerId,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] DateTime? rankingStartDate,
            [FromQuery] Engine? engine,
            [FromQuery] string country)
        {
            if (!CheckGameParameter(game))
            {
                return Json(new { error = "Invalid game value." });
            }

            if (playerId <= 0)
            {
                return Json(new { error = "Invalid player identifier." });
            }

            var players = await _statisticsProvider
                .GetPlayersAsync()
                .ConfigureAwait(false);
            if (!players.Any(p => p.Id == playerId))
            {
                return Json(new { error = "The player associated to the identifier does not exist." });
            }

            if (!CheckEngineParameter(engine))
            {
                return Json(new { error = "The engine is invalid." });
            }

            return await ViewAsync(
                PlayerDetailsViewName,
                $"PlayerID {playerId} - {game} times",
                async () =>
                {
                    var rankingEntries = await GetRankingsWithParamsAsync(game,
                            rankingDate ?? DateTime.Now, playerId, rankingStartDate, engine, country, false)
                        .ConfigureAwait(false);

                    return rankingEntries
                        .Single(r => r.Player.Id == playerId)
                        .ToPlayerDetailsViewData(StageImagePath);
                }).ConfigureAwait(false);
        }

        private static bool CheckGameParameter(Game game)
        {
            return Enum.TryParse(typeof(Game), game.ToString(), out _);
        }

        private static bool CheckEngineParameter(Engine? engine)
        {
            return !engine.HasValue || Enum.TryParse(typeof(Engine), engine.ToString(), out _);
        }

        private static bool CheckChronologyTypeParameter(ChronologyTypeItemData standingType)
        {
            return Enum.TryParse(typeof(ChronologyTypeItemData), standingType.ToString(), out _);
        }

        private static bool CheckStandingTypeParameter(StandingType standingType)
        {
            return Enum.TryParse(typeof(StandingType), standingType.ToString(), out _);
        }

        private async Task<IActionResult> SimulateRankingInternalAsync(
            Game game,
            DateTime? rankingDate,
            long? playerId = null,
            DateTime? rankingStartDate = null,
            Engine? engine = null,
            string country = null,
            bool countryGrouping = false)
        {
            return await ViewAsync(
                RankingViewName,
                "The GoldenEye/PerfectDark World Records and Rankings SIMULATOR",
                async () =>
                {
                    var rankingEntries = await GetRankingsWithParamsAsync(
                            game, rankingDate ?? DateTime.Now, playerId, rankingStartDate, engine, country, countryGrouping)
                        .ConfigureAwait(false);

                    var pointsRankingEntries = rankingEntries
                        .Where(r => r.Rank <= MaxRankDisplay)
                        .Select(r => r.ToPointsRankingItemData())
                        .ToList();

                    // this does not manage equality between two global times
                    // ie one player will be ranked above/below the other one
                    int rank = 1;
                    var timeRankingEntries = rankingEntries
                        .OrderBy(x => x.CumuledTime)
                        .Take(MaxRankDisplay)
                        .Select(r => r.ToTimeRankingItemData(rank++))
                        .ToList();

                    var secondsLevel = SystemExtensions.Enumerate<Level>().ToDictionary(l => l, l => 0);
                    var stageWorldRecordEntries = game.GetStages()
                        .Select(s => s.ToStageWorldRecordItemData(rankingEntries, secondsLevel, StageImagePath))
                        .ToList();

                    return new SimulatedRankingViewData
                    {
                        CombinedTime = new TimeSpan(0, 0, secondsLevel.Values.Sum()),
                        EasyCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Easy]),
                        MediumCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Medium]),
                        HardCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Hard]),
                        PointsRankingEntries = pointsRankingEntries,
                        TimeRankingEntries = timeRankingEntries,
                        StageWorldRecordEntries = stageWorldRecordEntries,
                        EasyLabel = Level.Easy.GetLabel(game),
                        MediumLabel = Level.Medium.GetLabel(game),
                        HardLabel = Level.Hard.GetLabel(game),
                        EasyShortLabel = Level.Easy.GetLabel(game, true),
                        MediumShortLabel = Level.Medium.GetLabel(game, true),
                        HardShortLabel = Level.Hard.GetLabel(game, true)
                    };
                }).ConfigureAwait(false);
        }

        private async Task<List<RankingEntry>> GetRankingsWithParamsAsync(
            Game game,
            DateTime rankingDate,
            long? playerId,
            DateTime? rankingStartDate,
            Engine? engine,
            string country,
            bool countryGrouping)
        {
            var request = new RankingRequest
            {
                Game = game,
                FullDetails = true,
                RankingDate = rankingDate,
                Engine = engine,
                IncludeUnknownEngine = true,
                RankingStartDate = rankingStartDate,
                Country = country,
                CountryGrouping = countryGrouping
            };

            if (playerId.HasValue)
            {
                request.PlayerVsLegacy = (playerId.Value, rankingDate);
                request.RankingDate = DateTime.Now;
            }

            var rankingEntriesBase = await _statisticsProvider
                .GetRankingEntriesAsync(request)
                .ConfigureAwait(false);

            return rankingEntriesBase.Select(r => r as RankingEntry).ToList();
        }

        private async Task<IActionResult> ViewAsync(
            string viewName,
            string title,
            Func<Task<object>> getDatasFunc)
        {
            try
            {
                return View($"Elite/Views/Template.cshtml", new BaseViewData
                {
                    Data = await getDatasFunc().ConfigureAwait(false),
                    Name = $"~/Elite/Views/{viewName}.cshtml",
                    Title = title
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"An error has occured:\n{ex.Message}" });
            }
        }
    }
}
