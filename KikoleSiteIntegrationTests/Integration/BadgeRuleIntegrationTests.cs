using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Requete de badge stockee en base : lecture seule, filtre facultatif sur l'utilisateur,
/// et refus de tout ce qui n'est pas un SELECT unique.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class BadgeRuleIntegrationTests
{
    private const string Rule =
        "SELECT user_id, MIN(proposal_date) AS badge_date FROM leaders " +
        "WHERE (@userId IS NULL OR user_id = @userId) GROUP BY user_id";

    private readonly DatabaseFixture _fixture;

    public BadgeRuleIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private BadgeRepository Repository() => new(_fixture.Configuration, _fixture.Clock);

    [Fact]
    public async Task ExecuteBadgeRuleAsync_WithoutUser_ReturnsEveryMatchingUserWithItsEarliestDate()
    {
        var matches = await Repository().ExecuteBadgeRuleAsync(Rule, null);

        matches.Should().HaveCountGreaterThan(1);
        matches.Select(m => m.UserId).Should().OnlyHaveUniqueItems();
        matches.Should().OnlyContain(m => m.BadgeDate != default);
    }

    [Fact]
    public async Task ExecuteBadgeRuleAsync_WithAUser_ReturnsOnlyThatUser()
    {
        var all = await Repository().ExecuteBadgeRuleAsync(Rule, null);
        var expected = all.First();

        var matches = await Repository().ExecuteBadgeRuleAsync(Rule, expected.UserId);

        matches.Should().ContainSingle().Which.Should().Be(expected);
    }

    [Fact]
    public async Task ExecuteBadgeRuleAsync_RefusesAWriteStatementAndTouchesNothing()
    {
        using var connection = _fixture.OpenConnection();
        var before = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM leaders");

        var matches = await Repository().ExecuteBadgeRuleAsync("DELETE FROM leaders", null);

        matches.Should().BeEmpty();
        (await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM leaders")).Should().Be(before);
    }
}