using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using KikoleApi.Interfaces;
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

        protected async Task<IReadOnlyList<T>> ExecuteReaderAsync<T>(string sql, object parameters)
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

        protected async Task ExecuteInsertAsync(string table, params (string column, object value)[] columns)
        {
            await ExecuteNonQueryAsync(
                    GetBasicInsertSql(table, columns),
                    GetDynamicParameters(columns))
                .ConfigureAwait(false);
        }

        protected async Task<T> GetDtoAsync<T>(string table, params (string column, object value)[] conditions)
        {
            return await ExecuteScalarAsync<T>(
                    GetBasicSelectSql(table, conditions),
                    GetDynamicParameters(conditions))
                .ConfigureAwait(false);
        }

        protected async Task<IReadOnlyList<T>> GetDtosAsync<T>(string table, params (string column, object value)[] conditions)
        {
            return await ExecuteReaderAsync<T>(
                    GetBasicSelectSql(table, conditions),
                    GetDynamicParameters(conditions))
                .ConfigureAwait(false);
        }

        private static string GetBasicSelectSql(string table, (string column, object value)[] conditions)
        {
            return $"SELECT * FROM {table} WHERE {string.Join(" AND ", conditions.Select(c => $"{c.column} = @{c.column}"))}";
        }

        private static string GetBasicInsertSql(string table, (string column, object value)[] columns)
        {
            return $"INSERT INTO {table} ({string.Join(", ", columns.Select(c => c.column))}) VALUES ({string.Join(", ", columns.Select(c => $"@{c.column}"))})";
        }

        private static DynamicParameters GetDynamicParameters((string column, object value)[] conditions)
        {
            var parameters = new DynamicParameters();
            for (var i = 0; i < conditions.Length; i++)
                parameters.Add(conditions[i].column, conditions[i].value);
            return parameters;
        }
    }
}
