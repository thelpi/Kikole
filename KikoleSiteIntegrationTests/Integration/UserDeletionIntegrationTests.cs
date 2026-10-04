using System;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Suppression physique d'un compte : tout ce qui lui appartient disparaît, ses kikolés publiés
/// sont repris par le premier administrateur, ses kikolés en attente sont supprimés.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class UserDeletionIntegrationTests
{
    // l'administrateur de kikole_mock.sql
    private const ulong AdminId = 1;

    private readonly DatabaseFixture _fixture;

    public UserDeletionIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteUserWithAllDataAsync_RemovesTheUserAndHisDataButKeepsValidatedPlayers()
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var playerRepository = new PlayerRepository(_fixture.Configuration, _fixture.Clock);
        var proposalRepository = new ProposalRepository(_fixture.Configuration, _fixture.Clock);
        var leaderRepository = new LeaderRepository(_fixture.Configuration, _fixture.Clock);

        var userId = await userRepository.CreateUserAsync(UserDtoBuilder.Valid().WithLogin("integration_to_erase").Build());
        var godchildId = await userRepository.CreateUserAsync(
            UserDtoBuilder.Valid().WithLogin("integration_godchild").Build() with { SponsorUserId = userId });

        var publishedId = await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Published Player").WithAllowedNames("published player").WithCreator(userId)
                .WithPublicationDate(new DateOnly(2031, 1, 1)).Build());
        var pendingId = await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Pending Player").WithAllowedNames("pending player").WithCreator(userId).Build());
        await playerRepository.CreatePlayerClubsAsync(new KikoleSite.Models.Dtos.PlayerClubDto { PlayerId = pendingId, ClubId = 1, HistoryPosition = 1 });

        await proposalRepository.CreateProposalAsync(ProposalDtoBuilder.Valid().WithUser(userId).WithProposalDate(new DateOnly(2031, 1, 2)).Build());
        await leaderRepository.CreateLeaderAsync(LeaderDtoBuilder.Valid().WithUser(userId).OnTheDay(new DateOnly(2031, 1, 2), 5).Build());
        await userRepository.CreateLoginHistoryAsync(userId, "10.0.0.9");

        using var connection = _fixture.OpenConnection();
        await connection.ExecuteAsync("INSERT INTO user_badges (user_id, badge_id, get_date) VALUES (@userId, 1, CURDATE())", new { userId });
        await connection.ExecuteAsync("INSERT INTO registration_guids (id, user_id) VALUES (UUID(), @userId)", new { userId });
        await connection.ExecuteAsync("INSERT INTO discussions (user_id, creation_date) VALUES (@userId, NOW())", new { userId });
        await connection.ExecuteAsync(
            "INSERT INTO discussion_messages (discussion_id, message, creation_date, is_from_admin, is_read) " +
            "SELECT id, 'bonjour', NOW(), 0, 0 FROM discussions WHERE user_id = @userId", new { userId });

        var deleted = await userRepository.DeleteUserWithAllDataAsync(userId);

        deleted.Should().BeTrue();
        (await CountAsync(connection, "SELECT COUNT(*) FROM users WHERE id = @userId", userId)).Should().Be(0);
        foreach (var table in new[] { "leaders", "proposals", "user_badges", "login_history", "registration_guids", "discussions" })
            (await CountAsync(connection, $"SELECT COUNT(*) FROM {table} WHERE user_id = @userId", userId)).Should().Be(0, table);
        (await CountAsync(connection, "SELECT COUNT(*) FROM discussion_messages WHERE message = 'bonjour'", userId)).Should().Be(0);

        (await connection.ExecuteScalarAsync<ulong>("SELECT creation_user_id FROM players WHERE id = @publishedId", new { publishedId })).Should().Be(AdminId);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM players WHERE id = @pendingId", new { pendingId })).Should().Be(0);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM player_clubs WHERE player_id = @pendingId", new { pendingId })).Should().Be(0);
        (await connection.ExecuteScalarAsync<ulong?>("SELECT sponsor_user_id FROM users WHERE id = @godchildId", new { godchildId })).Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserWithAllDataAsync_RefusesAnAdministratorAndAnUnknownAccount()
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);

        (await userRepository.DeleteUserWithAllDataAsync(AdminId)).Should().BeFalse();
        (await userRepository.DeleteUserWithAllDataAsync(987654321)).Should().BeFalse();

        (await userRepository.GetUserByIdAsync(AdminId)).Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteUserWithAllDataAsync_WhenSubmittedTwiceAtOnce_DeletesOnceAndFailsCleanly()
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var userId = await userRepository.CreateUserAsync(UserDtoBuilder.Valid().WithLogin("integration_double_submit").Build());

        var results = await Task.WhenAll(
            userRepository.DeleteUserWithAllDataAsync(userId),
            userRepository.DeleteUserWithAllDataAsync(userId));

        results.Should().BeEquivalentTo(new[] { true, false });
        (await userRepository.GetUserByIdIncludingDisabledAsync(userId)).Should().BeNull();
    }
    private static async Task<int> CountAsync(MySqlConnector.MySqlConnection connection, string sql, ulong userId)
        => await connection.ExecuteScalarAsync<int>(sql, new { userId });
}
