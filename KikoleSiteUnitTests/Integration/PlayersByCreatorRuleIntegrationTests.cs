using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteUnitTests.Integration;

/// <summary>
/// Caracterise <see cref="PlayerRepository.GetPlayersByCreatorAsync"/> : l'etat d'une
/// soumission (en attente / acceptee / rejetee) est encode dans un seul parametre
/// <c>@type</c> 0/1/2 derive de <c>bool? accepted</c>. Seul <c>accepted: true</c> est
/// appele en production aujourd'hui (badges, page « mes soumissions ») ; <c>false</c> et
/// <c>null</c> font partie de l'interface publique sans appelant actuel — caracterises ici
/// pour ne pas les laisser sans filet si un futur appelant les utilise.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class PlayersByCreatorRuleIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public PlayersByCreatorRuleIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetPlayersByCreatorAsync_FiltersByPublicationOrRejectionState()
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var playerRepository = new PlayerRepository(_fixture.Configuration, _fixture.Clock);

        var creatorId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_creator").Build());
        var otherCreatorId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_other_creator").Build());

        var pendingId = await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Pending Player").WithAllowedNames("pending player").WithCreator(creatorId).Build());
        var acceptedId = await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Accepted Player").WithAllowedNames("accepted player").WithCreator(creatorId)
                .WithPublicationDate(_fixture.Clock.Today).Build());
        var rejectedId = await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Rejected Player").WithAllowedNames("rejected player").WithCreator(creatorId).Build());
        // reject_date n'est pas ecrit a la creation (CreatePlayerAsync ne le prend pas en
        // charge) : un rejet passe toujours par ce second appel, apres coup.
        await playerRepository.RefusePlayerProposalAsync(rejectedId);
        // un autre createur : ne doit jamais apparaitre, quel que soit le filtre
        await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Other's Player").WithAllowedNames("other's player").WithCreator(otherCreatorId)
                .WithPublicationDate(_fixture.Clock.Today).Build());

        var any = await playerRepository.GetPlayersByCreatorAsync(creatorId, null);
        var accepted = await playerRepository.GetPlayersByCreatorAsync(creatorId, true);
        var rejected = await playerRepository.GetPlayersByCreatorAsync(creatorId, false);

        any.Select(p => p.Id).Should().BeEquivalentTo([pendingId, acceptedId, rejectedId]);
        accepted.Select(p => p.Id).Should().BeEquivalentTo([acceptedId]);
        rejected.Select(p => p.Id).Should().BeEquivalentTo([rejectedId]);
    }
}
