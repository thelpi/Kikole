using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;
using KikoleSite.Elite.Models;
using KikoleSite.Elite.Providers;
using KikoleSite.Elite.ViewDatas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        private readonly IStatisticsProvider _statisticsProvider;
        private readonly Api.Interfaces.IClock _clock;

        public SimulatedRankingController(
            IStatisticsProvider statisticsProvider,
            Api.Interfaces.IClock clock)
        {
            _statisticsProvider = statisticsProvider;
            _clock = clock;
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync()
        {
            return await IndexAsync(null).ConfigureAwait(false);
        }

        private async Task<IActionResult> IndexAsync(string errorMessage)
        {
            return await ViewAsync(
                    "Index",
                    "The Elite infographics",
                    async () =>
                    {
                        var players = await _statisticsProvider
                            .GetPlayersAsync(useCache: true)
                            .ConfigureAwait(false);

                        var countries = players
                            .Select(p => p.Country)
                            .Distinct()
                            .Where(c => !string.IsNullOrWhiteSpace(c))
                            .OrderBy(c => c)
                            .Select(c => new SelectListItem(c, c))
                            .ToList();

                        countries.Insert(0, new SelectListItem(string.Empty, null));

                        return new IndexViewData
                        {
                            Countries = countries,
                            Game = (int)Game.GoldenEye,
                            StandingType = (int)StandingType.Untied,
                            ChronologyType = (int)ChronologyTypeItemData.FirstUnslay,
                            ErrorMessage = errorMessage
                        };
                    })
                .ConfigureAwait(false);
        }

        [HttpGet("players/{playerId}")]
        public async Task<IActionResult> GetPlayerDetailsAsync(
            [FromRoute] long playerId,
            [FromQuery] Game game)
        {
            if (!CheckGameParameter(game))
                return await IndexAsync("Invalid game value.").ConfigureAwait(false);

            var (success, p) = await CheckPlayerParameterAsync(
                    playerId, true)
                .ConfigureAwait(false);
            if (!success)
                return await IndexAsync("Invalid player.").ConfigureAwait(false);

            return await ViewAsync(
                "Player",
                $"Player details for {game}",
                async () =>
                {
                    var untiedWrs = await _statisticsProvider
                        .GetLongestStandingsAsync(game, null, StandingType.Untied, null, null, p.Id, null)
                        .ConfigureAwait(false);
                    var slayUntiedWrs = await _statisticsProvider
                        .GetLongestStandingsAsync(game, null, StandingType.Untied, null, null, null, p.Id)
                        .ConfigureAwait(false);
                    var slayWrs = await _statisticsProvider
                        .GetLongestStandingsAsync(game, null, StandingType.FirstUnslayed, null, null, null, p.Id)
                        .ConfigureAwait(false);
                    var allWrs = await _statisticsProvider
                        .GetLongestStandingsAsync(game, null, StandingType.FirstUnslayed, null, null, p.Id, null)
                        .ConfigureAwait(false);

                    return new PlayerViewData
                    {
                        Game = game,
                        Color = $"#{p.Color}",
                        Country = p.Country,
                        RealName = p.RealName,
                        SurName = p.SurName,
                        WorldRecords = new PlayerWorldRecordsItemData
                        {
                            UntiedSlayWorldRecords = slayUntiedWrs
                                .Select(_ => _.ToStandingItemData())
                                .ToList(),
                            UntiedWorldRecords = untiedWrs
                                .Select(_ => _.ToStandingItemData())
                                .ToList(),
                            WorldRecords = allWrs
                                .Select(_ => _.ToStandingItemData())
                                .ToList(),
                            SlayWorldRecords = slayWrs
                                .Select(_ => _.ToStandingItemData())
                                .ToList()
                        },
                        // TODO
                        JoinDate = new DateTime(2012, 04, 05),
                        LastActivityDate = DateTime.Today,
                        BestPointsRank = (1, 5850, new DateTime(2015, 12, 02)),
                        BestTimeRank = (4, new TimeSpan(1, 24, 36), new DateTime(2017, 08, 02)),
                        RankingMilestones = new List<(DateTime, int)>(),
                        RankingPointsMilestones = new List<(DateTime, int)>(),
                        RankingHistory = new List<(DateTime, int)>()
                    };
                }).ConfigureAwait(false);
        }

        [HttpGet("players")]
        public async Task<IActionResult> GetPlayersAsync()
        {
            return await ViewAsync(
                "Players",
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

        [HttpGet("games/{game}/chronology-types/{chronologyType}/data")]
        public async Task<JsonResult> GetChronologyDataAsync(
            [FromRoute] Game game,
            [FromRoute] ChronologyTypeItemData chronologyType,
            [FromQuery] Engine? engine,
            [FromQuery] long? playerId,
            [FromQuery] byte anonymise)
        {
            if (!CheckGameParameter(game))
                return Json(new { error = "Invalid game value." });

            if (!CheckEngineParameter(engine))
                return Json(new { error = "The engine is invalid." });

            if (!CheckChronologyTypeParameter(chronologyType))
                return Json(new { error = "The chronology type is invalid." });

            var (success, _) = await CheckPlayerParameterAsync(
                    playerId, chronologyType.IsFullStage())
                .ConfigureAwait(false);
            if (!success)
                return Json(new { error = "Invalid player." });

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
                    .GetLongestStandingsAsync(game, null, chronologyType.ToStandingType(playerId.HasValue).Value, null, engine, playerId, null)
                    .ConfigureAwait(false);

                results = standings
                    .Select(_ => _.ToChronologyCanvasItemData(anonymise != 0, AnonymiseColorRgb))
                    .ToList();
            }

            return Json(results);
        }

        [HttpPost("player-filter")]
        public async Task<JsonResult> GetPlayersJsonAsync(string pattern)
        {
            var players = await _statisticsProvider
                .GetPlayersAsync(useCache: true, pattern: pattern)
                .ConfigureAwait(false);

            return Json(players.Select(p => new
            {
                id = p.Id,
                color = p.Color,
                name = $"{p.RealName} - {p.SurName}"
            }));
        }

        #region Features

        [HttpPost("longest-standing-world-records")]
        public async Task<IActionResult> GetLongestStandingAsync(IndexViewData viewData)
        {
            if (viewData == null)
                return await IndexAsync("Invalid form.").ConfigureAwait(false);

            if (!CheckGameParameter(viewData.Game, out var game))
                return await IndexAsync("Invalid game value.").ConfigureAwait(false);

            if (!CheckEngineParameter(viewData.Engine, out var engine))
                return await IndexAsync("The engine is invalid.").ConfigureAwait(false);

            if (!CheckStandingTypeParameter(viewData.StandingType, out var standingType))
                return await IndexAsync("The standing type is invalid.").ConfigureAwait(false);

            var (success, p) = await CheckPlayerParameterAsync(
                    viewData.PlayerId, false)
                .ConfigureAwait(false);
            if (!success)
                return await IndexAsync("Invalid player.").ConfigureAwait(false);

            var (successSlayer, pSlayer) = await CheckPlayerParameterAsync(
                    viewData.SlayerPlayerId, false)
                .ConfigureAwait(false);
            if (!successSlayer)
                return await IndexAsync("Invalid slayer player.").ConfigureAwait(false);

            var title = $"{game} - {standingType.GetStandingTypeDescription()}";
            if (engine.HasValue)
                title += $" - {engine} only";
            if (p != null)
                title += $" - {p.ToString(game)}";
            if (pSlayer != null)
                title += $" - slayed/tied by {pSlayer.ToString(game)}";
            if (viewData.RankingDate.HasValue)
                title += $" - {viewData.RankingDate.Value:yyyy-MM-dd}";
            if (viewData.StillOngoing > 0)
                title += " - Only ongoing entries";
            else if (viewData.StillOngoing < 0)
                title += " - Only finished entries";

            return await ViewAsync(
                    "LongestStanding",
                    title,
                    async () =>
                    {
                        var results = await _statisticsProvider
                            .GetLongestStandingsAsync(
                                game,
                                viewData.RankingDate,
                                standingType,
                                viewData.StillOngoing > 0
                                    ? true
                                    : (viewData.StillOngoing < 0
                                        ? false
                                        : default(bool?)),
                                engine,
                                viewData.PlayerId,
                                viewData.SlayerPlayerId)
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

        [HttpPost("world-records-chronology")]
        public async Task<IActionResult> GetWorldRecordsChronologyAsync(IndexViewData viewData)
        {
            if (viewData == null)
                return await IndexAsync("Invalid form.").ConfigureAwait(false);

            if (!CheckGameParameter(viewData.Game, out var game))
                return await IndexAsync("Invalid game value.").ConfigureAwait(false);

            if (!CheckChronologyTypeParameter(viewData.ChronologyType, out var type) || type.IsFullStage())
                return await IndexAsync("The chronology type is invalid.").ConfigureAwait(false);

            if (!CheckEngineParameter(viewData.Engine, out var engine))
                return await IndexAsync("The engine is invalid.").ConfigureAwait(false);

            var (success, _) = await CheckPlayerParameterAsync(
                    viewData.PlayerId, false)
                .ConfigureAwait(false);
            if (!success)
                return await IndexAsync("Invalid player.").ConfigureAwait(false);

            var model = new ChronologyCanvasViewData
            {
                TotalDays = (int)Math.Floor((_clock.Tomorrow - game.GetEliteFirstDate()).TotalDays),
                ChronologyType = type,
                Engine = engine,
                Game = game,
                PlayerId = viewData.PlayerId,
                Anonymise = viewData.Anonymise,
                StageImages = game.GetStages().ToDictionary(_ => _, _ => string.Format(StageImagePath, (int)_))
            };

            return View($"Elite/Views/ChronologyCanvas.cshtml", model);
        }

        [HttpPost("rankings-chronology")]
        public async Task<IActionResult> GetRankingsChronologyAsync(IndexViewData viewData)
        {
            if (viewData == null)
                return await IndexAsync("Invalid form.").ConfigureAwait(false);

            if (!CheckGameParameter(viewData.Game, out var game))
                return await IndexAsync("Invalid game value.").ConfigureAwait(false);

            if (!CheckChronologyTypeParameter(viewData.ChronologyType, out var type) || !type.IsFullStage())
                return await IndexAsync("The chronology type is invalid.").ConfigureAwait(false);

            var (success, _) = await CheckPlayerParameterAsync(
                    viewData.PlayerId, true)
                .ConfigureAwait(false);
            if (!success)
                return await IndexAsync("Invalid player.").ConfigureAwait(false);

            var model = new ChronologyCanvasViewData
            {
                TotalDays = (int)Math.Floor((_clock.Tomorrow - game.GetEliteFirstDate()).TotalDays),
                ChronologyType = type,
                Game = game,
                PlayerId = viewData.PlayerId,
                Anonymise = viewData.Anonymise,
                StageImages = game.GetStages().ToDictionary(_ => _, _ => string.Format(StageImagePath, (int)_))
            };

            return View($"Elite/Views/ChronologyCanvas.cshtml", model);
        }

        [HttpPost("player-dystopia-rankings")]
        public async Task<IActionResult> GetRankingByPlayerDystopiaAsync(IndexViewData viewData)
        {
            if (viewData == null)
                return await IndexAsync("Invalid form.").ConfigureAwait(false);

            if (!CheckGameParameter(viewData.Game, out var game))
                return await IndexAsync("Invalid game value.").ConfigureAwait(false);

            var (success, player) = await CheckPlayerParameterAsync(
                    viewData.PlayerId, true)
                .ConfigureAwait(false);
            if (!success)
                return await IndexAsync("Invalid player.").ConfigureAwait(false);

            return await SimulateRankingInternalAsync(
                    game, viewData.RankingDate, player)
                .ConfigureAwait(false);
        }

        [HttpPost("time-frame-rankings")]
        public async Task<IActionResult> GetRankingByTimeFrameAsync(IndexViewData viewData)
        {
            if (viewData == null)
                return await IndexAsync("Invalid form.").ConfigureAwait(false);

            if (!CheckGameParameter(viewData.Game, out var game))
                return await IndexAsync("Invalid game value.").ConfigureAwait(false);

            if (!CheckRankingStartDateParameter(viewData))
                return await IndexAsync("The ranking start date is invalid.").ConfigureAwait(false);

            return await SimulateRankingInternalAsync(
                    game, viewData.RankingDate, rankingStartDate: viewData.RankingStartDate)
                .ConfigureAwait(false);
        }

        [HttpPost("engine-rankings")]
        public async Task<IActionResult> GetRankingByEngineAsync(IndexViewData viewData)
        {
            if (viewData == null)
                return await IndexAsync("Invalid form.").ConfigureAwait(false);

            if (!CheckGameParameter(viewData.Game, out var game))
                return await IndexAsync("Invalid game value.").ConfigureAwait(false);

            if (!viewData.Engine.HasValue || !CheckEngineParameter(viewData.Engine, out var engine))
                return await IndexAsync("The engine is invalid.").ConfigureAwait(false);

            return await SimulateRankingInternalAsync(
                    game, viewData.RankingDate, engine: engine)
                .ConfigureAwait(false);
        }

        [HttpPost("country-rankings")]
        public async Task<IActionResult> GetRankingByCountryAsync(IndexViewData viewData)
        {
            if (viewData == null)
                return await IndexAsync("Invalid form.").ConfigureAwait(false);

            if (!CheckGameParameter(viewData.Game, out var game))
                return await IndexAsync("Invalid game value.").ConfigureAwait(false);

            return await SimulateRankingInternalAsync(
                    game,
                    viewData.RankingDate,
                    country: viewData.Country,
                    countryGrouping: string.IsNullOrWhiteSpace(viewData.Country))
                .ConfigureAwait(false);
        }

        [HttpPost("player-ranking-details")]
        public async Task<IActionResult> GetPlayerRankingDetailsAsync(IndexViewData viewData)
        {
            if (viewData == null)
                return await IndexAsync("Invalid form.").ConfigureAwait(false);

            if (CheckGameParameter(viewData.Game, out var game))
                return await IndexAsync("Invalid game value.").ConfigureAwait(false);

            var (success, _) = await CheckPlayerParameterAsync(viewData.PlayerId, true).ConfigureAwait(false);
            if (!success)
                return await IndexAsync("Invalid player.").ConfigureAwait(false);

            if (!CheckEngineParameter(viewData.Engine, out var engine))
                return await IndexAsync("The engine is invalid.").ConfigureAwait(false);

            return await ViewAsync(
                "PlayerRankingDetails",
                // TODO: customize the title
                $"PlayerID {viewData.PlayerId} - {game} times",
                async () =>
                {
                    var rankingEntries = await GetRankingsWithParamsAsync(game,
                            viewData.RankingDate ?? _clock.Today,
                            viewData.PlayerId,
                            viewData.RankingStartDate,
                            engine,
                            viewData.Country,
                            false)
                        .ConfigureAwait(false);

                    return rankingEntries
                        .Single(r => r.Player.Id == viewData.PlayerId)
                        .ToPlayerRankingDetailsViewData(StageImagePath);
                }).ConfigureAwait(false);
        }

        #endregion Features

        #region Old routes for features

        [HttpGet("games/{game}/longest-standings")]
        public async Task<IActionResult> GetLongestStandingAsync(
            [FromRoute] Game game,
            [FromQuery] bool? stillOngoing,
            [FromQuery] long? playerId,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] StandingType standingType,
            [FromQuery] Engine? engine,
            [FromQuery] long? slayerPlayerId)
        {
            return await GetLongestStandingAsync(
                new IndexViewData
                {
                    RankingDate = rankingDate,
                    StandingType = (int)standingType,
                    Game = (int)game,
                    StillOngoing = stillOngoing == true
                        ? 1
                        : (stillOngoing == false
                            ? -1
                            : 0),
                    PlayerId = playerId,
                    Engine = (int?)engine,
                    SlayerPlayerId = slayerPlayerId
                })
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/player-rankings")]
        public async Task<IActionResult> GetRankingByPlayerAsync(
            [FromRoute] Game game,
            [FromQuery] long playerId,
            [FromQuery] DateTime? rankingDate)
        {
            return await GetRankingByPlayerDystopiaAsync(
                new IndexViewData
                {
                    Game = (int)game,
                    PlayerId = playerId,
                    RankingDate = rankingDate
                })
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/time-frame-rankings")]
        public async Task<IActionResult> GetRankingByTimeFrameAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] DateTime rankingStartDate)
        {
            return await GetRankingByTimeFrameAsync(
                new IndexViewData
                {
                    Game = (int)game,
                    RankingDate = rankingDate,
                    RankingStartDate = rankingStartDate
                })
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/engine-rankings")]
        public async Task<IActionResult> GetRankingByEngineAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] Engine engine)
        {
            return await GetRankingByEngineAsync(
                new IndexViewData
                {
                    Game = (int)game,
                    RankingDate = rankingDate,
                    Engine = (int)engine
                })
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/country-rankings")]
        public async Task<IActionResult> GetRankingByCountryAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] string country)
        {
            return await GetRankingByCountryAsync(
                new IndexViewData
                {
                    Game = (int)game,
                    RankingDate = rankingDate,
                    Country = country
                })
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/players/{playerId}/ranking-details")]
        public async Task<IActionResult> GetPlayerRankingDetailsAsync(
            [FromRoute] Game game,
            [FromRoute] long playerId,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] DateTime? rankingStartDate,
            [FromQuery] Engine? engine,
            [FromQuery] string country)
        {
            return await GetPlayerRankingDetailsAsync(
                new IndexViewData
                {
                    Game = (int)game,
                    RankingDate = rankingDate,
                    Country = country,
                    PlayerId = playerId,
                    RankingStartDate = rankingStartDate,
                    Engine = (int?)engine
                })
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/wr-chronology")]
        public async Task<IActionResult> GetWorldRecordsChronologyAsync(
            [FromRoute] Game game,
            [FromQuery] ChronologyTypeItemData type,
            [FromQuery] Engine? engine,
            [FromQuery] long? playerId,
            [FromQuery] bool anonymise)
        {
            return await GetWorldRecordsChronologyAsync(
                new IndexViewData
                {
                    Game = (int)game,
                    PlayerId = playerId,
                    Engine = (int?)engine,
                    Anonymise = anonymise,
                    ChronologyType = (int)type
                })
                .ConfigureAwait(false);
        }

        [HttpGet("games/{game}/rk-chronology")]
        public async Task<IActionResult> GetRankingsChronologyAsync(
            [FromRoute] Game game,
            [FromQuery] ChronologyTypeItemData type,
            [FromQuery] long playerId,
            [FromQuery] bool anonymise)
        {
            return await GetRankingsChronologyAsync(
                new IndexViewData
                {
                    Game = (int)game,
                    PlayerId = playerId,
                    Anonymise = anonymise,
                    ChronologyType = (int)type
                })
                .ConfigureAwait(false);
        }

        #endregion Old routes for features

        private async Task<IActionResult> SimulateRankingInternalAsync(
            Game game,
            DateTime? rankingDate,
            Player player = null,
            DateTime? rankingStartDate = null,
            Engine? engine = null,
            string country = null,
            bool countryGrouping = false)
        {
            var title = $"{game} - Ranking generator";
            if (countryGrouping)
                title += " - by country";
            if (!string.IsNullOrWhiteSpace(country))
                title += $" - {country}";
            if (engine.HasValue)
                title += $" - {engine} only";
            if (player != null)
                title += $" - {player.ToString(game)}";
            if (rankingStartDate.HasValue)
                title += $" - starts at {rankingStartDate.Value:yyyy-MM-dd}";
            if (rankingDate.HasValue)
                title += $" - {rankingDate.Value:yyyy-MM-dd}";

            return await ViewAsync(
                "SimulatedRanking",
                title,
                async () =>
                {
                    var rankingEntries = await GetRankingsWithParamsAsync(
                            game, rankingDate ?? _clock.Today, player?.Id, rankingStartDate, engine, country, countryGrouping)
                        .ConfigureAwait(false);

                    var pointsRankingEntries = rankingEntries
                        .Where(r => r.Rank <= MaxRankDisplay)
                        .Select(r => r.ToPointsRankingItemData())
                        .ToList();

                    // proceed to a new orderBy from the base list
                    var timeRankingEntries = rankingEntries
                        .OrderBy(x => x.CumuledTime)
                        .ToList()
                        .WithRanks(r => r.CumuledTime)
                        .Where(r => r.Rank <= MaxRankDisplay)
                        .Select(r => r.ToTimeRankingItemData())
                        .ToList();

                    // accumulate time at each call of 'ToStageWorldRecordItemData' below
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
                request.RankingDate = _clock.Today;
            }

            var rankingEntriesBase = await _statisticsProvider
                .GetRankingEntriesAsync(request)
                .ConfigureAwait(false);

            return rankingEntriesBase.Cast<RankingEntry>().ToList();
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
                return await IndexAsync(ex.Message).ConfigureAwait(false);
            }
        }

        #region Check parameters methods

        private async Task<(bool success, Player data)> CheckPlayerParameterAsync(long? playerId, bool required)
        {
            if (!playerId.HasValue)
                return (!required, null);

            if (playerId <= 0)
                return (false, null);

            var players = await _statisticsProvider
                .GetPlayersAsync()
                .ConfigureAwait(false);
            var p = players.SingleOrDefault(p => p.Id == playerId);
            return (p != null, p);
        }

        private static bool CheckGameParameter(int gameId, out Game game)
        {
            var match = SystemExtensions.Enumerate<Game>().Where(g => (int)g == gameId);
            game = match.FirstOrDefault();
            return match.Any();
        }

        private static bool CheckGameParameter(Game game)
        {
            return Enum.TryParse(typeof(Game), game.ToString(), out _);
        }

        private static bool CheckEngineParameter(int? engineId, out Engine? engine)
        {
            if (!engineId.HasValue)
            {
                engine = null;
                return true;
            }
            var match = SystemExtensions.Enumerate<Engine>().Where(e => (int)e == engineId);
            engine = match.FirstOrDefault();
            return match.Any();
        }

        private static bool CheckEngineParameter(Engine? engine)
        {
            return !engine.HasValue || Enum.TryParse(typeof(Engine), engine.ToString(), out _);
        }

        private static bool CheckChronologyTypeParameter(int chronologyTypeId, out ChronologyTypeItemData chronologyType)
        {
            var match = SystemExtensions.Enumerate<ChronologyTypeItemData>().Where(ct => (int)ct == chronologyTypeId);
            chronologyType = match.FirstOrDefault();
            return match.Any();
        }

        private static bool CheckChronologyTypeParameter(ChronologyTypeItemData standingType)
        {
            return Enum.TryParse(typeof(ChronologyTypeItemData), standingType.ToString(), out _);
        }

        private static bool CheckStandingTypeParameter(int standingTypeId, out StandingType standingType)
        {
            var match = SystemExtensions.Enumerate<StandingType>().Where(st => (int)st == standingTypeId);
            standingType = match.FirstOrDefault();
            return match.Any();
        }

        private bool CheckRankingStartDateParameter(IndexViewData viewData)
        {
            return viewData.RankingStartDate.HasValue
                && viewData.RankingStartDate < _clock.Tomorrow
                && (!viewData.RankingDate.HasValue || viewData.RankingDate >= viewData.RankingStartDate);
        }

        #endregion Check parameters methods
    }
}
