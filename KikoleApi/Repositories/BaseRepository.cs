using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using KikoleApi.Abstractions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace KikoleApi.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly IClock Clock;

        private readonly string _connectionString;
        private const string ConnectionStringName = "Kikole";

        protected BaseRepository(IConfiguration configuration, IClock clock)
        {
            _connectionString = configuration.GetConnectionString(ConnectionStringName);
            Clock = clock;
        }

        protected async Task ExecuteNonQueryAsync(string sql, object parameters)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection
                    .QueryAsync(
                        sql,
                        parameters,
                        commandType: CommandType.Text)
                    .ConfigureAwait(false);
            }
        }

        protected async Task<T> ExecuteScalarAsync<T>(string sql, object parameters, T defaultValue = default(T))
        {
            T result = defaultValue;

            using (var connection = new MySqlConnection(_connectionString))
            {
                var results = await connection
                    .QueryAsync<T>(
                        sql,
                        parameters,
                        commandType: CommandType.Text)
                    .ConfigureAwait(false);

                if (results.Any())
                    result = results.First();
            }

            return result;
        }

        protected async Task<IReadOnlyCollection<T>> ExecuteReaderAsync<T>(string sql, object parameters)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                return (await connection
                    .QueryAsync<T>(
                        sql,
                        parameters,
                        commandType: CommandType.Text)
                    .ConfigureAwait(false)).ToList();
            }
        }

        protected async Task<ulong> GetLastInsertedIdAsync()
        {
            return await ExecuteScalarAsync<ulong>("SELECT LAST_INSERT_ID()", null)
                .ConfigureAwait(false);
        }

        protected async Task ExecuteInsertAsync(string table, string[] columns, object[] values)
        {
            var sql = $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(c => $"@{c}"))})";

            var parameters = new DynamicParameters();
            for (var i = 0; i < columns.Length; i++)
                parameters.Add(columns[i], values[i]);

            await ExecuteNonQueryAsync(sql, parameters).ConfigureAwait(false);
        }
    }
}
