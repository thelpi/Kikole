using System;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Une désactivation garde sa date et sa raison, et aucune opération ne réactive un compte.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class UserDisableIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public UserDisableIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DisableUserAsync_KeepsDateAndReason_AndHidesTheUserFromNormalLookups()
    {
        var repository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var id = await repository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_to_disable").WithEmailHash("hash-integration-to-disable").Build());

        await repository.DisableUserAsync(id, "multi-compte");

        (await repository.GetUserByIdAsync(id)).Should().BeNull();
        var user = await repository.GetUserByIdIncludingDisabledAsync(id);
        user!.IsDisabled.Should().BeTrue();
        user.DisabledReason.Should().Be("multi-compte");
        user.DisabledDate.Should().BeCloseTo(_fixture.Clock.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateUserAsync_NeverReactivatesADisabledUser()
    {
        var repository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var id = await repository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_stays_disabled").WithEmailHash("hash-integration-stays-disabled").Build());
        await repository.DisableUserAsync(id, "abus");
        var user = (await repository.GetUserByIdIncludingDisabledAsync(id))!;

        await repository.UpdateUserAsync(user with { IsDisabled = false });

        (await repository.GetUserByIdIncludingDisabledAsync(id))!.IsDisabled.Should().BeTrue();
    }
}
