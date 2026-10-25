using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Suppression d'un club : refusee s'il est dans la carriere d'un kikole ou vise par une
/// proposition reussie ; sinon ses propositions incorrectes gardent son nom au lieu de son identifiant.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class ClubDeletionIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public ClubDeletionIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private ClubRepository Clubs() => new(_fixture.Configuration, _fixture.Clock);

    // les clubs refuses restent en base d'un lancement a l'autre : un nom unique evite le doublon (name, country_id)
    private async Task<(ulong Id, string Name)> CreateClubAsync(string baseName)
    {
        var name = $"{baseName} {Guid.NewGuid().ToString("N")[..8]}";
        var id = await Clubs().CreateClubAsync(new ClubDto { Name = name, CountryId = (ulong)Countries.FRA });
        await Clubs().ReplaceClubTranslationsAsync(id,
        [
            new ClubTranslationDto { ClubId = id, LanguageId = 1, Priority = 0, Name = name },
            new ClubTranslationDto { ClubId = id, LanguageId = 2, Priority = 0, Name = name },
            new ClubTranslationDto { ClubId = id, LanguageId = 2, Priority = 1, Name = name + " alias" }
        ]);
        return (id, name);
    }

    private async Task<ulong> CreateProposalAsync(ulong userId, ulong clubId, byte successful)
    {
        var repository = new ProposalRepository(_fixture.Configuration, _fixture.Clock);
        return await repository.CreateProposalAsync(
            ProposalDtoBuilder.Valid().WithUser(userId).OfType(ProposalTypes.Club)
                .WithValue(clubId.ToString()).WithSuccessfulFlag(successful)
                .WithProposalDate(_fixture.Clock.Today).WithCreationDate(_fixture.Clock.Now).Build());
    }

    private async Task<ulong> CreateUserAsync(string login)
    {
        return await new UserRepository(_fixture.Configuration, _fixture.Clock)
            .CreateUserAsync(UserDtoBuilder.Valid().WithLogin(login).Build());
    }

    private async Task<string?> ProposalValueAsync(ulong proposalId)
    {
        using var connection = _fixture.OpenConnection();
        return await connection.ExecuteScalarAsync<string?>("SELECT value FROM proposals WHERE id = @proposalId", new { proposalId });
    }

    [Fact]
    public async Task DeleteClubAsync_ForAnUnusedClub_RemovesItAndItsNames()
    {
        var (clubId, _) = await CreateClubAsync("Club supprime");

        var result = await Clubs().DeleteClubAsync(clubId);

        result.Outcome.Should().Be(ClubDeletionOutcomes.Deleted);
        (await Clubs().GetClubAsync(clubId)).Should().BeNull();
        (await Clubs().GetClubTranslationsAsync()).Should().NotContain(t => t.ClubId == clubId);
    }

    [Fact]
    public async Task DeleteClubAsync_ForAClubOfAKikole_RefusesAndKeepsEverything()
    {
        var userId = await CreateUserAsync("integration_club_player");
        var playerRepository = new PlayerRepository(_fixture.Configuration, _fixture.Clock);
        var playerId = await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Joueur du club").WithAllowedNames("joueur").WithCreator(userId).Build());
        var (clubId, _) = await CreateClubAsync("Club utilise");
        await playerRepository.CreatePlayerClubsAsync(new PlayerClubDto { PlayerId = playerId, ClubId = clubId, HistoryPosition = 1 });

        var result = await Clubs().DeleteClubAsync(clubId);

        result.Outcome.Should().Be(ClubDeletionOutcomes.UsedByPlayers);
        (await Clubs().GetClubAsync(clubId)).Should().NotBeNull();
        (await Clubs().GetPlayerNamesUsingClubAsync(clubId)).Should().Equal("Joueur du club");
    }

    [Fact]
    public async Task DeleteClubAsync_KeepsTheIncorrectProposalsUnderTheNameOfTheClub()
    {
        var userId = await CreateUserAsync("integration_club_wrong");
        var (clubId, clubName) = await CreateClubAsync("Club a nom conserve");
        var proposalId = await CreateProposalAsync(userId, clubId, successful: 0);

        var usage = await Clubs().GetClubUsageAsync(clubId);
        var result = await Clubs().DeleteClubAsync(clubId);

        usage.PlayerNames.Should().BeEmpty();
        usage.IncorrectProposals.Should().Be(1);
        usage.CorrectProposals.Should().Be(0);
        result.Should().Be(new ClubDeletionResult(ClubDeletionOutcomes.Deleted, 1));
        (await ProposalValueAsync(proposalId)).Should().Be(clubName);
    }

    [Fact]
    public async Task DeleteClubAsync_ForAClubTargetedByACorrectProposal_RefusesAndTouchesNothing()
    {
        var userId = await CreateUserAsync("integration_club_right");
        var (clubId, _) = await CreateClubAsync("Club juste");
        var correct = await CreateProposalAsync(userId, clubId, successful: 1);
        var wrong = await CreateProposalAsync(userId, clubId, successful: 0);

        var result = await Clubs().DeleteClubAsync(clubId);

        result.Outcome.Should().Be(ClubDeletionOutcomes.UsedByCorrectProposals);
        (await Clubs().GetClubAsync(clubId)).Should().NotBeNull();
        (await ProposalValueAsync(correct)).Should().Be(clubId.ToString());
        (await ProposalValueAsync(wrong)).Should().Be(clubId.ToString());
    }

    [Fact]
    public async Task DeleteClubAsync_ForAnUnknownClub_ReportsNotFound()
    {
        var result = await Clubs().DeleteClubAsync(999_999_999);

        result.Outcome.Should().Be(ClubDeletionOutcomes.NotFound);
    }
}