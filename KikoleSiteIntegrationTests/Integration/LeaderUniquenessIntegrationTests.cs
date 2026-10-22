using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Un utilisateur n'a qu'une victoire par jour : la contrainte unique de <c>leaders</c>
/// protège contre deux requêtes simultanées (double clic), le dépôt renvoie alors 0.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class LeaderUniquenessIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public LeaderUniquenessIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateLeaderAsync_WhenTheUserAlreadyWonThatDay_ReturnsZeroAndKeepsASingleRow()
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var leaderRepository = new LeaderRepository(_fixture.Configuration, _fixture.Clock);

        var day = _fixture.Clock.Today.AddDays(-14);
        var userId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_double_win").Build());

        var first = await leaderRepository.CreateLeaderAsync(
            LeaderDtoBuilder.Valid().WithUser(userId).OnTheDay(day, 5).Build());
        var second = await leaderRepository.CreateLeaderAsync(
            LeaderDtoBuilder.Valid().WithUser(userId).OnTheDay(day, 5).Build());

        first.Should().BeGreaterThan(0UL);
        second.Should().Be(0UL);
        (await leaderRepository.GetUserLeadersAsync(day, day, false, userId)).Should().ContainSingle();
    }
}