﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Elite.Repositories
{
    public sealed class ReadRepository : BaseRepository, IReadRepository
    {
        public ReadRepository(IConfiguration configuration) : base(configuration) { }

        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue || endDate.HasValue)
            {
                return await GetEntriesByCriteriaInternalAsync(stage, level, startDate, endDate).ConfigureAwait(false);
            }

            return await GetEntriesByCriteriaInternalAsync(stage, level, null, null).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync(bool banned = false)
        {
            return await ExecuteReaderAsync<PlayerDto>(
                    "SELECT id, url_name, real_name, surname,  color, " +
                    "control_style, is_dirty, is_banned " +
                    "FROM player " +
                    "WHERE is_banned = @is_banned",
                    new
                    {
                        is_banned = banned
                    })
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyCollection<EntryDto>> GetEntriesByCriteriaInternalAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate)
        {
            return await ExecuteReaderAsync<EntryDto>(
                    "SELECT id, date, level_id AS Level, player_id, " +
                    "stage_id AS Stage, system_id AS Engine, time " +
                    "FROM entry " +
                    "WHERE (@start_date IS NULL OR date >= @start_date) " +
                    "AND (@end_date IS NULL OR date < @end_date) " +
                    "AND (@stage_id IS NULL OR stage_id = @stage_id) " +
                    "AND (@level_id IS NULL OR level_id = @level_id)",
                    new
                    {
                        stage_id = (long?)stage,
                        level_id = (int?)level,
                        start_date = startDate,
                        end_date = endDate
                    })
                .ConfigureAwait(false);
        }
    }
}
