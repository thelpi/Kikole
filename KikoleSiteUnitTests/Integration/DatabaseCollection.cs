using Xunit;

namespace KikoleSiteUnitTests.Integration;

/// <summary>
/// Une seule instance de <see cref="DatabaseFixture"/> (et donc un seul reset via
/// kikole_mock.sql) partagee par toutes les classes de tests d'integration.
/// </summary>
[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
