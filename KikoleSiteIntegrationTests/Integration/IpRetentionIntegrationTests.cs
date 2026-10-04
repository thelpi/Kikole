using System;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Les adresses IP au-delà de la durée de conservation sont effacées (historique de connexion
/// supprimé, IP d'inscription et de proposition mises à NULL) ; les plus récentes sont gardées.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class IpRetentionIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public IpRetentionIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PurgeAndClear_OnlyTouchRowsOlderThanTheCutoff()
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var proposalRepository = new ProposalRepository(_fixture.Configuration, _fixture.Clock);

        var oldUserId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_old_ip").WithIp("10.0.0.1").Build());
        var recentUserId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_recent_ip").WithIp("10.0.0.2").Build());

        await userRepository.CreateLoginHistoryAsync(oldUserId, "10.0.0.1");
        await userRepository.CreateLoginHistoryAsync(recentUserId, "10.0.0.2");
        await proposalRepository.CreateProposalAsync(ProposalDtoBuilder.Valid().WithUser(oldUserId).WithIp("10.0.0.1").Build());
        await proposalRepository.CreateProposalAsync(ProposalDtoBuilder.Valid().WithUser(recentUserId).WithIp("10.0.0.2").Build());

        // vieillit les lignes de l'utilisateur « ancien » (les depots datent toujours de maintenant)
        var longAgo = _fixture.Clock.Now.AddYears(-2);
        using var connection = _fixture.OpenConnection();
        await connection.ExecuteAsync("UPDATE users SET creation_date = @longAgo WHERE id = @oldUserId", new { longAgo, oldUserId });
        await connection.ExecuteAsync("UPDATE login_history SET creation_date = @longAgo WHERE user_id = @oldUserId", new { longAgo, oldUserId });
        await connection.ExecuteAsync("UPDATE proposals SET creation_date = @longAgo WHERE user_id = @oldUserId", new { longAgo, oldUserId });

        var cutoff = _fixture.Clock.Now.AddYears(-1);
        await userRepository.PurgeIpAddressesAsync(cutoff);
        await proposalRepository.ClearIpAddressesAsync(cutoff);

        (await connection.ExecuteScalarAsync<string?>("SELECT ip FROM users WHERE id = @oldUserId", new { oldUserId })).Should().BeNull();
        (await connection.ExecuteScalarAsync<string?>("SELECT ip FROM users WHERE id = @recentUserId", new { recentUserId })).Should().Be("10.0.0.2");
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM login_history WHERE user_id = @oldUserId", new { oldUserId })).Should().Be(0);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM login_history WHERE user_id = @recentUserId", new { recentUserId })).Should().Be(1);
        (await connection.ExecuteScalarAsync<string?>("SELECT ip FROM proposals WHERE user_id = @oldUserId", new { oldUserId })).Should().BeNull();
        (await connection.ExecuteScalarAsync<string?>("SELECT ip FROM proposals WHERE user_id = @recentUserId", new { recentUserId })).Should().Be("10.0.0.2");
    }
}
