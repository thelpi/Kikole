using System;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using KikoleSite;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Xunit;

namespace KikoleSiteUnitTests.Integration;

/// <summary>
/// Connecte les tests d'integration a la vraie base MySQL locale (WAMP) et la remet a
/// l'etat de <c>kikole_mock.sql</c> avant chaque classe de tests qui la partage (voir
/// <see cref="DatabaseCollection"/>) : ce script est idempotent (TRUNCATE puis re-INSERT),
/// meme mecanisme que les smoke tests manuels.
///
/// Chaine de connexion en *user-secrets*, propre a ce projet (UserSecretsId distinct de
/// KikoleSite) : `dotnet user-secrets set "ConnectionStrings:Kikole" "..."` depuis
/// KikoleSiteUnitTests/. Sans WAMP demarre et ce secret pose, ces tests echouent a la
/// connexion : c'est le seul filet, pas de skip automatique (cf. TODO.md).
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public IConfiguration Configuration { get; }

    public IClock Clock { get; } = new Clock();

    public string ConnectionString { get; }

    public DatabaseFixture()
    {
        // pose une fois par process de test : Program.cs le fait pour l'appli, jamais
        // execute ici, et sans lui Dapper ne mappe aucune colonne snake_case (user_id -> ...).
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        Configuration = new ConfigurationBuilder()
            .AddUserSecrets<DatabaseFixture>()
            .Build();

        ConnectionString = Configuration.GetConnectionString("Kikole")
            ?? throw new InvalidOperationException(
                "Chaine de connexion 'Kikole' absente : " +
                "dotnet user-secrets set \"ConnectionStrings:Kikole\" \"...\" depuis KikoleSiteUnitTests/.");
    }

    public async Task InitializeAsync()
    {
        var mockDataScript = File.ReadAllText(FindMockDataScriptPath());

        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(mockDataScript);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public MySqlConnection OpenConnection()
    {
        var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    private static string FindMockDataScriptPath()
    {
        const string fileName = "kikole_mock.sql";

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, fileName)))
            directory = directory.Parent;

        if (directory == null)
            throw new InvalidOperationException(
                $"'{fileName}' introuvable en remontant depuis '{AppContext.BaseDirectory}'.");

        return Path.Combine(directory.FullName, fileName);
    }
}
