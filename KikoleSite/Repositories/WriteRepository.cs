﻿using System.Threading.Tasks;
using KikoleSite.Dtos;
using KikoleSite.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories
{
    public sealed class WriteRepository : BaseRepository, IWriteRepository
    {
        public WriteRepository(IClock clock, IConfiguration configuration)
            : base(configuration, clock) { }

        public async Task<uint> ReplaceTimeEntryAsync(EntryDto requestEntry)
        {
            return await ExecuteNonQueryAndGetInsertedIdAsync<uint>(
                    "REPLACE INTO entries " +
                    "(player_id, level_id, stage_id, date, time, system_id, creation_date) " +
                    "VALUES " +
                    "(@player_id, @level_id, @stage_id, @date, @time, @system_id, NOW())",
                    new
                    {
                        player_id = requestEntry.PlayerId,
                        level_id = (byte)requestEntry.Level,
                        stage_id = (byte)requestEntry.Stage,
                        requestEntry.Date,
                        requestEntry.Time,
                        system_id = (byte)requestEntry.Engine
                    })
                .ConfigureAwait(false);
        }

        public async Task<uint> InsertPlayerAsync(string urlName, string defaultHexColor)
        {
            return await ExecuteNonQueryAndGetInsertedIdAsync<uint>(
                    "INSERT INTO players " +
                    "(url_name, real_name, surname, color, control_style, creation_date) " +
                    "VALUES " +
                    "(@url_name, @real_name, @surname, @color, @control_style, NOW())",
                    new
                    {
                        url_name = urlName,
                        real_name = urlName,
                        surname = urlName,
                        color = defaultHexColor,
                        control_style = default(string)
                    })
                .ConfigureAwait(false);
        }

        public async Task DeletePlayerEntriesAsync(Game game, uint playerId)
        {
            await ExecuteNonQueryAsync(
                    $"DELETE FROM entries WHERE player_id = @player_id AND stage_id {(game == Game.GoldenEye ? "<= 20" : "> 20")}",
                    new
                    {
                        player_id = playerId
                    })
                .ConfigureAwait(false);
        }

        public async Task UpdatePlayerAsync(PlayerDto player)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE players " +
                    "SET real_name = @real_name, " +
                    "   surname = @surname, " +
                    "   color = @color, " +
                    "   control_style = @control_style, " +
                    "   is_banned = 0, " +
                    "   country = @country, " +
                    "   min_year_of_birth = @minyob, " +
                    "   max_year_of_birth = @maxyob " +
                    "WHERE id = @id",
                    new
                    {
                        id = player.Id,
                        real_name = player.RealName,
                        surname = player.SurName,
                        color = player.Color,
                        control_style = player.ControlStyle,
                        country = player.Country,
                        minyob = player.MinYearOfBirth,
                        maxyob = player.MaxYearOfBirth
                    })
                .ConfigureAwait(false);
        }

        public async Task BanPlayerAsync(uint playerId)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE players " +
                    "SET is_banned = 1 " +
                    "WHERE id = @id",
                    new
                    {
                        id = playerId
                    })
                .ConfigureAwait(false);
        }

        public async Task DeleteEntriesAsync(params uint[] entriesId)
        {
            if (entriesId?.Length > 0)
            {
                await ExecuteNonQueryAsync(
                        $"DELETE FROM entries WHERE id IN ({string.Join(",", entriesId)})",
                        null)
                    .ConfigureAwait(false);
            }
        }
    }
}
