using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Dtos;
using KikoleSite.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories
{
    public sealed class ReadRepository : BaseRepository, IReadRepository
    {
        private IReadOnlyList<PlayerDto> _playersCache = null;
        private readonly object _cacheLock = new object();

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
                lock (_cacheLock)
                {
                    if (_playersCache != null)
                    {
                        return _playersCache;
                    }
                }
            }

            var players = await ExecuteReaderAsync<PlayerDto>(
                    "SELECT id, url_name, real_name, surname,  color, control_style, is_banned, country " +
                    "FROM players " +
                    "WHERE is_banned = @is_banned",
                    new
                    {
                        is_banned = banned
                    })
                .ConfigureAwait(false);

            if (!banned)
            {
                lock (_cacheLock)
                {
                    _playersCache = players;
                }
            }

            return players;
        }

        public async Task<IReadOnlyCollection<EntryDto>> GetPlayerEntriesAsync(uint playerId, Game game)
        {
            return await GetEntriesByCriteriaInternalAsync(
                    null, null, null, null, playerId, game)
                .ConfigureAwait(false);
        }

        public async Task<RankingDto> GetRankingAsync(Stage stage, Level level, DateTime date, NoDateEntryRankingRule noDateRule)
        {
            var rankings = await ExecuteReaderAsync<RankingDto>(
                    "SELECT id, date, rule_id AS Rule, stage_id AS Stage, level_id AS Level " +
                    "FROM rankings " +
                    "WHERE date <= @date AND rule_id = @rule_id AND stage_id = @stage_id AND level_id = @level_id " +
                    "ORDER BY date DESC " +
                    "LIMIT 0, 1",
                    new
                    {
                        stage_id = (byte)stage,
                        level_id = (byte)level,
                        date,
                        rule_id = (byte)noDateRule
                    })
                .ConfigureAwait(false);

            return rankings.FirstOrDefault();
        }

        public async Task<IReadOnlyList<RankingEntryDto>> GetRankingEntriesAsync(uint rankingId)
        {
            return await ExecuteReaderAsync<RankingEntryDto>(
                    "SELECT ranking_id, player_id, points, rank, time, entry_id, entry_date " +
                    "FROM ranking_entries " +
                    "WHERE ranking_id = @ranking_id",
                    new { ranking_id = rankingId })
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyCollection<EntryDto>> GetEntriesByCriteriaInternalAsync(
            Stage? stage, Level? level, DateTime? startDate, DateTime? endDate, uint? playerId, Game? game)
        {
            return await ExecuteReaderAsync<EntryDto>(
                    "SELECT id, date, level_id AS Level, player_id, " +
                    "stage_id AS Stage, system_id AS Engine, time " +
                    "FROM entries " +
                    "WHERE (@start_date IS NULL OR date >= @start_date) " +
                    "AND (@end_date IS NULL OR date < @end_date) " +
                    "AND (@stage_id IS NULL OR stage_id = @stage_id) " +
                    "AND (@level_id IS NULL OR level_id = @level_id) " +
                    "AND (@player_id IS NULL OR player_id = @player_id) " +
                    "AND (" + (game.HasValue ? (game == Game.GoldenEye ? "stage_id <= 20" : "stage_id > 20") : "1=1") + ") ",
                    new
                    {
                        stage_id = (byte?)stage,
                        level_id = (byte?)level,
                        start_date = startDate,
                        end_date = endDate,
                        player_id = playerId
                    })
                .ConfigureAwait(false);
        }
    }
}
