using System;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Une demande de suppression garde sa date et son motif, ne s'enregistre qu'une fois, et se
/// classe sans toucher au compte.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class UserDeletionRequestIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public UserDeletionRequestIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(UserRepository repository, ulong id)> CreateUserAsync(string login, UserTypes type = UserTypes.StandardUser)
    {
        var repository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var id = await repository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin(login).WithEmailHash("hash-" + login).WithType(type).Build());

        return (repository, id);
    }

    [Fact]
    public async Task RequestAccountDeletionAsync_KeepsDateAndReason_AndOnlyAcceptsOneRequest()
    {
        var (repository, id) = await CreateUserAsync("integration_delete_request");

        (await repository.RequestAccountDeletionAsync(id, "je pars")).Should().BeTrue();
        (await repository.RequestAccountDeletionAsync(id, "autre motif")).Should().BeFalse();

        var user = await repository.GetUserByIdAsync(id);
        user!.DeletionRequested.Should().BeTrue();
        user.DeletionReason.Should().Be("je pars");
        user.DeletionRequestedDate.Should().BeCloseTo(_fixture.Clock.Now, TimeSpan.FromSeconds(2));
        (await repository.HasPendingDeletionRequestsAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task RequestAccountDeletionAsync_WithoutReason_StoresNone()
    {
        var (repository, id) = await CreateUserAsync("integration_delete_noreason");

        (await repository.RequestAccountDeletionAsync(id, null)).Should().BeTrue();

        (await repository.GetUserByIdAsync(id))!.DeletionReason.Should().BeNull();
    }

    [Fact]
    public async Task RequestAccountDeletionAsync_RefusesAnAdministratorAndADisabledAccount()
    {
        var (repository, adminId) = await CreateUserAsync("integration_delete_admin", UserTypes.Administrator);
        var (_, disabledId) = await CreateUserAsync("integration_delete_disabled");
        await repository.DisableUserAsync(disabledId, "abus");

        (await repository.RequestAccountDeletionAsync(adminId, null)).Should().BeFalse();
        (await repository.RequestAccountDeletionAsync(disabledId, null)).Should().BeFalse();
    }

    [Fact]
    public async Task DismissDeletionRequestAsync_ClearsTheRequestAndKeepsTheAccount()
    {
        var (repository, id) = await CreateUserAsync("integration_delete_dismiss");
        await repository.RequestAccountDeletionAsync(id, "je pars");

        (await repository.DismissDeletionRequestAsync(id)).Should().BeTrue();
        (await repository.DismissDeletionRequestAsync(id)).Should().BeFalse();

        var user = await repository.GetUserByIdAsync(id);
        user.Should().NotBeNull();
        user!.DeletionRequested.Should().BeFalse();
        user.DeletionReason.Should().BeNull();
        user.DeletionRequestedDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_KeepsThePendingDeletionRequest()
    {
        var (repository, id) = await CreateUserAsync("integration_delete_update");
        await repository.RequestAccountDeletionAsync(id, "je pars");
        var user = (await repository.GetUserByIdAsync(id))!;

        await repository.UpdateUserAsync(user with { Ip = "10.0.0.1" });

        (await repository.GetUserByIdAsync(id))!.DeletionRequested.Should().BeTrue();
    }
}