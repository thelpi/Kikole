using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Models.Dtos;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Caracterise <see cref="PlayerRepository.UpdatePlayerAsync"/> : la fiche, les clubs et les
/// indices traduits sont remplaces ensemble, et seul un kikole en attente ou programme apres
/// aujourd'hui est modifiable.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class PlayerUpdateIntegrationTests
{
    private const ulong French = 2;

    private readonly DatabaseFixture _fixture;

    public PlayerUpdateIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(PlayerRepository repository, ulong playerId)> CreatePlayerAsync(string login, DateOnly? publicationDate)
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var repository = new PlayerRepository(_fixture.Configuration, _fixture.Clock);

        var creatorId = await userRepository.CreateUserAsync(UserDtoBuilder.Valid().WithLogin(login).Build());
        var playerId = await repository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName("Avant").WithAllowedNames("avant").WithCreator(creatorId)
                .WithClues("clue avant", "easy avant").WithPublicationDate(publicationDate).Build());

        await repository.CreatePlayerClubsAsync(new PlayerClubDto { PlayerId = playerId, ClubId = 1, HistoryPosition = 1 });
        await repository.CreatePlayerClubsAsync(new PlayerClubDto { PlayerId = playerId, ClubId = 2, HistoryPosition = 2 });
        await repository.CreatePlayerClubsAsync(new PlayerClubDto { PlayerId = playerId, ClubId = 3, HistoryPosition = 3 });
        await repository.InsertPlayerCluesByLanguageAsync(playerId, 0, new Dictionary<ulong, string> { { French, "indice avant" } });
        await repository.InsertPlayerCluesByLanguageAsync(playerId, 1, new Dictionary<ulong, string> { { French, "facile avant" } });

        return (repository, playerId);
    }

    private static PlayerDto UpdatedPlayer(ulong creatorId) => PlayerDtoBuilder.Valid()
        .WithName("Apres").WithAllowedNames("apres;alias").WithYearOfBirth(1980)
        .WithClues("clue apres", "easy apres").WithHiddenCreator().WithCreator(creatorId).Build();

    [Fact]
    public async Task UpdatePlayerAsync_OnAScheduledPlayer_ReplacesTheSheetTheClubsAndTheTranslatedClues()
    {
        var (repository, playerId) = await CreatePlayerAsync("integration_update_scheduled", _fixture.Clock.Today.AddDays(500));
        var before = await repository.GetPlayerByIdAsync(playerId);

        var updated = await repository.UpdatePlayerAsync(
            playerId,
            UpdatedPlayer(0),
            [
                new PlayerClubDto { PlayerId = playerId, ClubId = 4, HistoryPosition = 1 },
                new PlayerClubDto { PlayerId = playerId, ClubId = 1, HistoryPosition = 2, IsLoan = 1 }
            ],
            new Dictionary<ulong, string> { { French, "indice apres" } },
            new Dictionary<ulong, string>());

        updated.Should().BeTrue();
        var after = await repository.GetPlayerByIdAsync(playerId);
        after!.Name.Should().Be("Apres");
        after.AllowedNames.Should().Be("apres;alias");
        after.YearOfBirth.Should().Be(1980);
        after.Clue.Should().Be("clue apres");
        after.EasyClue.Should().Be("easy apres");
        after.HideCreator.Should().Be(1);
        after.CreationUserId.Should().Be(before!.CreationUserId);
        after.PublicationDate.Should().Be(before.PublicationDate);

        var clubs = await repository.GetPlayerClubsAsync(playerId);
        clubs.OrderBy(c => c.HistoryPosition).Select(c => (c.ClubId, c.HistoryPosition, c.IsLoan))
            .Should().Equal((4UL, (byte)1, (byte)0), (1UL, (byte)2, (byte)1));

        (await repository.GetClueAsync(playerId, 0, French)).Should().Be("indice apres");
        (await repository.GetClueAsync(playerId, 1, French)).Should().BeNull();
    }

    [Fact]
    public async Task UpdatePlayerAsync_OnAPendingSubmission_IsAllowed()
    {
        var (repository, playerId) = await CreatePlayerAsync("integration_update_pending", null);

        var updated = await repository.UpdatePlayerAsync(
            playerId, UpdatedPlayer(0), [new PlayerClubDto { PlayerId = playerId, ClubId = 4, HistoryPosition = 1 }],
            new Dictionary<ulong, string>(), new Dictionary<ulong, string>());

        updated.Should().BeTrue();
        (await repository.GetPlayerByIdAsync(playerId))!.Name.Should().Be("Apres");
    }

    [Fact]
    public async Task UpdatePlayerAsync_OnAPlayerPublishedTodayOrRefused_ChangesNothing()
    {
        var (repository, todayId) = await CreatePlayerAsync("integration_update_today", _fixture.Clock.Today);
        var (_, refusedId) = await CreatePlayerAsync("integration_update_refused", null);
        await repository.RefusePlayerProposalAsync(refusedId);

        foreach (var playerId in new[] { todayId, refusedId })
        {
            var updated = await repository.UpdatePlayerAsync(
                playerId, UpdatedPlayer(0), [new PlayerClubDto { PlayerId = playerId, ClubId = 4, HistoryPosition = 1 }],
                new Dictionary<ulong, string>(), new Dictionary<ulong, string>());

            updated.Should().BeFalse();
            (await repository.GetPlayerByIdAsync(playerId))!.Name.Should().Be("Avant");
            (await repository.GetPlayerClubsAsync(playerId)).Should().HaveCount(3);
            (await repository.GetClueAsync(playerId, 0, French)).Should().Be("indice avant");
        }
    }
}