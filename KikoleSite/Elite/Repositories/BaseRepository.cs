using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace KikoleSite.Elite.Repositories
{
    public abstract class BaseRepository
    {
        private readonly string _connectionString;
        private const string ConnectionStringName = "TheElite";

        protected BaseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString(ConnectionStringName);
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

        protected async Task<T> ExecuteScalarAsync<T>(string sql, object parameters, T defaultValue = default)
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
            using var connection = new MySqlConnection(_connectionString);
            return (await connection
                .QueryAsync<T>(
                    sql,
                    parameters,
                    commandType: CommandType.Text)
                .ConfigureAwait(false)).ToList();
        }

        protected async Task<ulong> ExecuteInsertAsync(string table, params (string column, object value)[] columns)
        {
            return await ExecuteNonQueryAndGetInsertedIdAsync(
                    GetBasicInsertSql(table, columns),
                    GetDynamicParameters(columns))
                .ConfigureAwait(false);
        }

        protected async Task<ulong> ExecuteReplaceAsync(string table, params (string column, object value)[] columns)
        {
            return await ExecuteNonQueryAndGetInsertedIdAsync(
                    GetBasicInsertSql(table, columns, true),
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
            return $"SELECT * FROM {table} WHERE {(conditions?.Length > 0 ? string.Join(" AND ", conditions.Select(c => $"{c.column} = @{c.column}")) : "1=1")}";
        }

        private static string GetBasicInsertSql(string table, (string column, object value)[] columns, bool replace = false)
        {
            return $"{(replace ? "REPLACE" : "INSERT")} INTO {table} ({string.Join(", ", columns.Select(c => c.column))}) VALUES ({string.Join(", ", columns.Select(c => $"@{c.column}"))})";
        }

        private static DynamicParameters GetDynamicParameters((string column, object value)[] conditions)
        {
            if (!(conditions?.Length > 0))
                return null;

            var parameters = new DynamicParameters();
            for (var i = 0; i < conditions.Length; i++)
                parameters.Add(conditions[i].column, conditions[i].value);
            return parameters;
        }
    }
}
