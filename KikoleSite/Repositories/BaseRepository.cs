using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace KikoleSite.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly IClock Clock;

        private readonly string _connectionString;
        private const string ConnectionStringName = "TheElite";

        protected BaseRepository(IConfiguration configuration, IClock clock)
        {
            _connectionString = configuration.GetConnectionString(ConnectionStringName);
            Clock = clock;
        }

        protected async Task<ulong> ExecuteNonQueryAndGetInsertedIdAsync(string sql, object parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection
                .QueryAsync(
                    sql,
                    parameters,
                    commandType: CommandType.Text)
                .ConfigureAwait(false);

            var results = await connection
                .QueryAsync<ulong>(
                    "SELECT LAST_INSERT_ID()",
                    commandType: CommandType.Text)
                .ConfigureAwait(false);

            return results.FirstOrDefault();
        }

        protected async Task ExecuteNonQueryAsync(string sql, object parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection
                .QueryAsync(
                    sql,
                    parameters,
                    commandType: CommandType.Text)
                .ConfigureAwait(false);
        }

        protected async Task<IReadOnlyList<T>> ExecuteReaderAsync<T>(string sql, object parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            return (await connection
                .QueryAsync<T>(
                    sql,
                    parameters,
                    commandType: CommandType.Text)
                .ConfigureAwait(false)).ToList();
        }
    }
}
