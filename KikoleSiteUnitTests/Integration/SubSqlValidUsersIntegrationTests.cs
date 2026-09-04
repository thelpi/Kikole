using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteUnitTests.Integration;

/// <summary>
/// Caracterise <c>BaseRepository.SubSqlValidUsers</c> (« joueur classable » = ni
/// administrateur, ni desactive), injectee dans sept requetes depuis la classe de base.
/// Verifiee ici via <see cref="LeaderRepository.GetLeadersAtDateAsync"/>, le point d'entree
/// le plus direct — mais la regle est la meme partout ou elle est injectee.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class SubSqlValidUsersIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public SubSqlValidUsersIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetLeadersAtDateAsync_ExcludesAdministratorsAndDisabledUsers()
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var leaderRepository = new LeaderRepository(_fixture.Configuration, _fixture.Clock);

        // admin (id 1) et joueur1 (id 2) viennent de kikole_mock.sql ; seul un utilisateur
        // desactive doit etre cree ici.
        var disabledUserId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid()
                .WithLogin("integration_disabled")
                .WithDisabled()
                .Build());

        var day = _fixture.Clock.Today;

        await leaderRepository.CreateLeaderAsync(LeaderDtoBuilder.Valid().WithUser(1).OnTheDay(day, 5).Build());
        await leaderRepository.CreateLeaderAsync(LeaderDtoBuilder.Valid().WithUser(2).OnTheDay(day, 6).Build());
        await leaderRepository.CreateLeaderAsync(LeaderDtoBuilder.Valid().WithUser(disabledUserId).OnTheDay(day, 7).Build());

        var leaders = await leaderRepository.GetLeadersAtDateAsync(day, onTimeOnly: true);

        leaders.Select(l => l.UserId).Should().BeEquivalentTo([2ul]);
    }
}
