using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using KikoleSite.Api.Interfaces;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace KikoleSite.Api.Repositories
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseRepository
    {
        protected readonly IClock Clock;

        private readonly string _connectionString;
        private const string ConnectionStringName = "Kikole";

        protected string SubSqlValidUsers => $"SELECT u.id FROM users AS u " +
            $"WHERE u.user_type_id != {(ulong)UserTypes.Administrator} " +
            $"AND u.is_disabled = 0";

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

        private static string GetBasicInsertSql(string table, (string column, object value)[] columns)
        {
            return $"INSERT INTO {table} ({string.Join(", ", columns.Select(c => c.column))}) VALUES ({string.Join(", ", columns.Select(c => $"@{c.column}"))})";
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
