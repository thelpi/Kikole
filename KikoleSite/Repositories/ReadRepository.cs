using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Dtos;
using KikoleSite.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories
{
    public sealed class ReadRepository : KikoleSite.Repositories.BaseRepository, IReadRepository
    {
        private IReadOnlyList<PlayerDto> _playersCache = null;
        private readonly object cacheLock = new object();

        public ReadRepository(IClock clock, IConfiguration configuration)
            : base(configuration, clock) { }

        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue || endDate.HasValue)
            {
                return await GetEntriesByCriteriaInternalAsync(
                        stage, level, startDate, endDate, null, null)
                    .ConfigureAwait(false);
            }

            return await GetEntriesByCriteriaInternalAsync(
                    stage, level, null, null, null, null)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync(bool banned = false, bool fromCache = false)
        {
            if (!banned && fromCache && _playersCache != null)
            {
                lock (cacheLock)
                {
                    if (_playersCache != null)
                    {
                        return _playersCache;
                    }
                }
            }

            var players = await ExecuteReaderAsync<PlayerDto>(
                    "SELECT id, url_name, real_name, surname,  color, control_style, is_banned, country " +
                    "FROM elite_players " +
                    "WHERE is_banned = @is_banned",
                    new
                    {
                        is_banned = banned
                    })
                .ConfigureAwait(false);

            if (!banned)
            {
                lock (cacheLock)
                {
                    _playersCache = players;
                }
            }

            return players;
        }

        public async Task<IReadOnlyCollection<EntryDto>> GetPlayerEntriesAsync(long playerId, Game game)
        {
            return await GetEntriesByCriteriaInternalAsync(
                    null, null, null, null, playerId, game)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<StageLevelRankingDto>> GetStageLevelRankingsAsync(Stage stage, Level level, DateTime date)
        {
            return await ExecuteReaderAsync<StageLevelRankingDto>(
                    "SELECT * " +
                    "FROM elite_stage_level_rankings " +
                    "WHERE date = (" +
                    "   SELECT MAX(r2.date) " +
                    "   FROM elite_stage_level_rankings AS r2 " +
                    "   WHERE r2.stage_id = @stage_id " +
                    "   AND r2.level_id = @level_id " +
                    "   AND r2.date <= @date" +
                    ") " +
                    "AND stage_id = @stage_id " +
                    "AND level_id = @level_id",
                    new
                    {
                        stage_id = (long)stage,
                        level_id = (long)level,
                        date = date.Date
                    })
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyCollection<EntryDto>> GetEntriesByCriteriaInternalAsync(
            Stage? stage, Level? level, DateTime? startDate, DateTime? endDate, long? playerId, Game? game)
        {
            return await ExecuteReaderAsync<EntryDto>(
                    "SELECT id, date, level_id AS Level, player_id, " +
                    "stage_id AS Stage, system_id AS Engine, time " +
                    "FROM elite_entries " +
                    "WHERE (@start_date IS NULL OR date >= @start_date) " +
                    "AND (@end_date IS NULL OR date < @end_date) " +
                    "AND (@stage_id IS NULL OR stage_id = @stage_id) " +
                    "AND (@level_id IS NULL OR level_id = @level_id) " +
                    "AND (@player_id IS NULL OR player_id = @player_id) " +
                    "AND (" + (game.HasValue ? (game == Game.GoldenEye ? "stage_id <= 20" : "stage_id > 20") : "1=1") + ") ",
                    new
                    {
                        stage_id = (long?)stage,
                        level_id = (int?)level,
                        start_date = startDate,
                        end_date = endDate,
                        player_id = playerId
                    })
                .ConfigureAwait(false);
        }
    }
}
