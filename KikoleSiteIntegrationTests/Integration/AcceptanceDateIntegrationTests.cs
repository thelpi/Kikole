using System;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Caracterise l'ecriture de <c>players.acceptance_date</c> par <see cref="PlayerRepository"/> :
/// nulle pour une soumission en attente, posee a la validation par l'administrateur, et egale
/// a la date de creation pour un kikole cree directement date (administrateur). Cette colonne
/// alimente le recalcul des badges "Do it yourself" et "We are kikole".
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class AcceptanceDateIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public AcceptanceDateIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AcceptanceDate_IsNullWhilePending_SetOnValidation_AndEqualToCreationForDirectlyDatedPlayers()
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var playerRepository = new PlayerRepository(_fixture.Configuration, _fixture.Clock);

        var creatorId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_acceptance_creator").Build());

        var pendingId = await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Pending Acceptance").WithAllowedNames("pending acceptance").WithCreator(creatorId).Build());
        var datedId = await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Dated Acceptance").WithAllowedNames("dated acceptance").WithCreator(creatorId)
                .WithPublicationDate(_fixture.Clock.Today.AddDays(400)).Build());

        var pending = await playerRepository.GetPlayerByIdAsync(pendingId);
        var dated = await playerRepository.GetPlayerByIdAsync(datedId);

        pending!.AcceptanceDate.Should().BeNull();
        dated!.AcceptanceDate.Should().Be(dated.CreationDate);

        var before = DateTime.Now.AddMinutes(-1);
        await playerRepository.ValidatePlayerProposalAsync(pendingId, _fixture.Clock.Today.AddDays(401));
        var validated = await playerRepository.GetPlayerByIdAsync(pendingId);

        validated!.AcceptanceDate.Should().NotBeNull().And.BeOnOrAfter(before);
        validated.PublicationDate.Should().Be(_fixture.Clock.Today.AddDays(401));
    }
}
