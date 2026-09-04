using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteUnitTests.Integration;

/// <summary>
/// Caracterise <see cref="ProposalRepository.GetMissingUsersAsLeaderAsync"/> : un
/// utilisateur a trouve le joueur (proposition de type <see cref="ProposalTypes.Name"/>
/// reussie) mais n'a pas de ligne correspondante dans <c>leaders</c>. Sert de filet a
/// <c>LeaderService.ComputeMissingLeadersAsync</c> (reparation admin, `AdminController`) :
/// une definition fausse ferait louper des reparations, pas seulement un test qui echoue.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class MissingLeadersRuleIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public MissingLeadersRuleIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetMissingUsersAsLeaderAsync_OnlyReturnsSuccessfulNameProposalsWithoutLeaderRow()
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var proposalRepository = new ProposalRepository(_fixture.Configuration, _fixture.Clock);
        var leaderRepository = new LeaderRepository(_fixture.Configuration, _fixture.Clock);

        var day = _fixture.Clock.Today.AddDays(-15);

        var foundWithLeaderId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_found_with_leader").Build());
        var foundWithoutLeaderId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_found_without_leader").Build());
        var triedButFailedId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_tried_but_failed").Build());

        // trouve, avec sa ligne leaders : pas manquant
        await proposalRepository.CreateProposalAsync(
            ProposalDtoBuilder.Valid().WithUser(foundWithLeaderId).OfType(ProposalTypes.Name).Successful().WithProposalDate(day).Build());
        await leaderRepository.CreateLeaderAsync(
            LeaderDtoBuilder.Valid().WithUser(foundWithLeaderId).OnTheDay(day, 5).Build());

        // trouve, sans ligne leaders : c'est le cas que la regle doit remonter
        await proposalRepository.CreateProposalAsync(
            ProposalDtoBuilder.Valid().WithUser(foundWithoutLeaderId).OfType(ProposalTypes.Name).Successful().WithProposalDate(day).Build());

        // proposition echouee : jamais trouve, rien a reparer
        await proposalRepository.CreateProposalAsync(
            ProposalDtoBuilder.Valid().WithUser(triedButFailedId).OfType(ProposalTypes.Name).Successful(false).WithProposalDate(day).Build());

        var missing = await proposalRepository.GetMissingUsersAsLeaderAsync(day);

        missing.Should().BeEquivalentTo([foundWithoutLeaderId]);
    }
}
