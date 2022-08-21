using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace KikoleSite.Elite.Repositories
{
    public sealed class WriteRepository : Api.Repositories.BaseRepository, IWriteRepository
    {
        public WriteRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<long> InsertTimeEntryAsync(EntryDto requestEntry)
        {
            try
            {
                return (long)await ExecuteNonQueryAndGetInsertedIdAsync(
                        "INSERT INTO entry " +
                        "(player_id, level_id, stage_id, date, time, system_id, creation_date) " +
                        "VALUES " +
                        "(@player_id, @level_id, @stage_id, @date, @time, @system_id, NOW())",
                        new
                        {
                            player_id = requestEntry.PlayerId,
                            level_id = (long)requestEntry.Level,
                            stage_id = (long)requestEntry.Stage,
                            requestEntry.Date,
                            requestEntry.Time,
                            system_id = (long)requestEntry.Engine
                        })
                    .ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return 0;
            }
        }

        public async Task<long> InsertPlayerAsync(string urlName, string defaultHexColor)
        {
            return (long)await ExecuteNonQueryAndGetInsertedIdAsync(
                    "INSERT INTO player " +
                    "(url_name, real_name, surname, color, control_style, is_dirty, creation_date) " +
                    "VALUES " +
                    "(@url_name, @real_name, @surname, @color, @control_style, @is_dirty, NOW())",
                    new
                    {
                        url_name = urlName,
                        real_name = urlName,
                        surname = urlName,
                        color = defaultHexColor,
                        control_style = default(string),
                        is_dirty = 1
                    })
                .ConfigureAwait(false);
        }

        public async Task UpdateDirtyPlayerAsync(long playerId)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE player SET is_dirty = 1 WHERE id = @id",
                    new { id = playerId })
                .ConfigureAwait(false);
        }

        public async Task DeletePlayerStageEntriesAsync(Stage stage, long playerId)
        {
            await ExecuteNonQueryAsync(
                    "DELETE FROM entry WHERE player_id = @player_id AND stage_id = @stage_id",
                    new
                    {
                        stage_id = (long)stage,
                        player_id = playerId
                    })
                .ConfigureAwait(false);
        }

        public async Task CleanPlayerAsync(PlayerDto player)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE player " +
                    "SET real_name = @real_name, surname = @surname, color = @color, " +
                    "control_style = @control_style, is_dirty = 0 " +
                    "WHERE id = @id",
                    new
                    {
                        id = player.Id,
                        real_name = player.RealName,
                        surname = player.SurName,
                        color = player.Color,
                        control_style = player.ControlStyle
                    })
                .ConfigureAwait(false);
        }
    }
}
