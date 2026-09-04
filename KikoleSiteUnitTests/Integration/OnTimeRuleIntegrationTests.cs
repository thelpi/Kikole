using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteUnitTests.Integration;

/// <summary>
/// Caracterise la regle « trouve a temps » (<c>proposal_date = DATE(creation_date)</c>),
/// dupliquee en SQL a plusieurs endroits (<see cref="LeaderRepository"/>,
/// <see cref="ProposalRepository"/>). Verifiee ici via
/// <see cref="LeaderRepository.GetLeadersAtDateAsync"/>, qui expose directement le
/// parametre <c>onTimeOnly</c>.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class OnTimeRuleIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public OnTimeRuleIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetLeadersAtDateAsync_OnTimeOnly_ExcludesCatchUpFinds()
    {
        var leaderRepository = new LeaderRepository(_fixture.Configuration, _fixture.Clock);

        var day = _fixture.Clock.Today.AddDays(-10);

        // joueur1 (id 2) a trouve le jour meme ; joueur2 (id 3) a trouve en rattrapage,
        // plusieurs jours plus tard.
        await leaderRepository.CreateLeaderAsync(LeaderDtoBuilder.Valid().WithUser(2).OnTheDay(day, 8).Build());
        await leaderRepository.CreateLeaderAsync(LeaderDtoBuilder.Valid().WithUser(3).AsCatchUp(day).Build());

        var onTime = await leaderRepository.GetLeadersAtDateAsync(day, onTimeOnly: true);
        var everyone = await leaderRepository.GetLeadersAtDateAsync(day, onTimeOnly: false);

        onTime.Select(l => l.UserId).Should().BeEquivalentTo([2ul]);
        everyone.Select(l => l.UserId).Should().BeEquivalentTo([2ul, 3ul]);
    }
}
