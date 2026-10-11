using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Models.Dtos;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Caracterise <see cref="MessageRepository"/> : gestion des annonces (liste, lecture,
/// modification, suppression) et choix de l'annonce affichee quand plusieurs sont actives.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class MessageIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public MessageIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private MessageRepository Repository() => new(_fixture.Configuration, _fixture.Clock);

    private async Task<ulong> InsertAsync(string message, DateTime? from, DateTime? to)
    {
        var repository = Repository();
        await repository.InsertMessageAsync(new MessageDto
        {
            Message = message,
            DisplayFrom = from,
            DisplayTo = to,
            CreationDate = _fixture.Clock.Now
        });

        return (await repository.GetMessagesAsync()).First(m => m.Message == message).Id;
    }

    [Fact]
    public async Task Announcement_CanBeReadUpdatedAndDeleted()
    {
        var repository = Repository();
        var from = new DateTime(2040, 1, 1, 8, 0, 0);
        var id = await InsertAsync("integration_announcement_crud", from, null);

        var inserted = await repository.GetMessageByIdAsync(id);
        inserted!.Message.Should().Be("integration_announcement_crud");
        inserted.DisplayFrom.Should().Be(from);
        inserted.DisplayTo.Should().BeNull();

        await repository.UpdateMessageAsync(inserted with
        {
            Message = "integration_announcement_crud_edited",
            DisplayFrom = null,
            DisplayTo = from.AddDays(3)
        });

        var updated = await repository.GetMessageByIdAsync(id);
        updated!.Message.Should().Be("integration_announcement_crud_edited");
        updated.DisplayFrom.Should().BeNull();
        updated.DisplayTo.Should().Be(from.AddDays(3));
        updated.CreationDate.Should().Be(inserted.CreationDate);

        (await repository.GetMessagesAsync()).Select(m => m.Id).Should().Contain(id);

        await repository.DeleteMessageAsync(id);

        (await repository.GetMessageByIdAsync(id)).Should().BeNull();
        (await repository.GetMessagesAsync()).Select(m => m.Id).Should().NotContain(id);
    }

    [Fact]
    public async Task WhenSeveralAnnouncementsAreActive_OnlyTheMostRecentlyStartedOneIsSelected()
    {
        var repository = Repository();
        var day = new DateTime(2045, 6, 1, 12, 0, 0);
        var older = await InsertAsync("integration_announcement_older", day.AddDays(-3), day.AddDays(3));
        var newer = await InsertAsync("integration_announcement_newer", day.AddDays(-1), null);
        var withoutStart = await InsertAsync("integration_announcement_without_start", null, null);
        var notYet = await InsertAsync("integration_announcement_not_yet", day.AddHours(1), null);
        var ended = await InsertAsync("integration_announcement_ended", day.AddDays(-5), day.AddHours(-1));

        (await repository.GetMessageAsync(day))!.Id.Should().Be(newer);

        await repository.DeleteMessageAsync(newer);
        (await repository.GetMessageAsync(day))!.Id.Should().Be(older);

        await repository.DeleteMessageAsync(older);
        (await repository.GetMessageAsync(day))!.Id.Should().Be(withoutStart);

        await repository.DeleteMessageAsync(withoutStart);
        await repository.DeleteMessageAsync(notYet);
        await repository.DeleteMessageAsync(ended);
    }
}