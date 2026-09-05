using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Models.Dtos;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services;

/// <summary>
/// Fil de discussion unique par utilisateur (jamais initie par l'admin) : le point
/// sensible est le "get-or-create" (une seule discussion par utilisateur, creee au
/// premier message et pas avant) et le marquage lu/non-lu comme effet de bord de chaque
/// lecture, dans les deux sens.
/// </summary>
public class DiscussionServiceTests
{
    private const ulong UserId = 42;
    private const ulong DiscussionId = 7;

    private readonly Mock<IDiscussionRepository> _discussionRepository = new();
    private readonly DiscussionService _service;

    public DiscussionServiceTests()
    {
        _service = new DiscussionService(_discussionRepository.Object);
    }

    // ------------------------------------------------------------- fil utilisateur

    [Fact]
    public async Task GetOwnThreadAsync_WhenNoDiscussionExistsYet_ReturnsEmptyWithoutCreatingOne()
    {
        _discussionRepository
            .Setup(_ => _.GetDiscussionByUserIdAsync(UserId))
            .ReturnsAsync((DiscussionDto?)null);

        var messages = await _service.GetOwnThreadAsync(UserId);

        messages.Should().BeEmpty();
        _discussionRepository.Verify(_ => _.CreateDiscussionAsync(It.IsAny<ulong>()), Times.Never);
        _discussionRepository.Verify(_ => _.GetMessagesAsync(It.IsAny<ulong>()), Times.Never);
        _discussionRepository.Verify(_ => _.MarkMessagesAsReadAsync(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task GetOwnThreadAsync_WhenADiscussionExists_ReturnsItsMessagesAndMarksAdminMessagesRead()
    {
        _discussionRepository
            .Setup(_ => _.GetDiscussionByUserIdAsync(UserId))
            .ReturnsAsync(new DiscussionDto { Id = DiscussionId, UserId = UserId });

        var expected = new List<DiscussionMessageDto>
        {
            new() { Id = 1, DiscussionId = DiscussionId, Message = "hello", IsFromAdmin = false }
        };
        _discussionRepository
            .Setup(_ => _.GetMessagesAsync(DiscussionId))
            .ReturnsAsync(expected);

        var messages = await _service.GetOwnThreadAsync(UserId);

        messages.Should().BeEquivalentTo(expected);
        _discussionRepository.Verify(_ => _.MarkMessagesAsReadAsync(DiscussionId, /* fromAdmin: */ true), Times.Once);
    }

    [Fact]
    public async Task PostUserMessageAsync_WhenNoDiscussionExistsYet_CreatesOneThenPostsTheMessage()
    {
        _discussionRepository
            .Setup(_ => _.GetDiscussionByUserIdAsync(UserId))
            .ReturnsAsync((DiscussionDto?)null);
        _discussionRepository
            .Setup(_ => _.CreateDiscussionAsync(UserId))
            .ReturnsAsync(DiscussionId);

        await _service.PostUserMessageAsync(UserId, "hello");

        _discussionRepository.Verify(_ => _.CreateDiscussionAsync(UserId), Times.Once);
        _discussionRepository.Verify(_ => _.CreateMessageAsync(DiscussionId, "hello", /* isFromAdmin: */ false), Times.Once);
    }

    [Fact]
    public async Task PostUserMessageAsync_WhenADiscussionAlreadyExists_ReusesItWithoutCreatingAnother()
    {
        _discussionRepository
            .Setup(_ => _.GetDiscussionByUserIdAsync(UserId))
            .ReturnsAsync(new DiscussionDto { Id = DiscussionId, UserId = UserId });

        await _service.PostUserMessageAsync(UserId, "second message");

        _discussionRepository.Verify(_ => _.CreateDiscussionAsync(It.IsAny<ulong>()), Times.Never);
        _discussionRepository.Verify(_ => _.CreateMessageAsync(DiscussionId, "second message", false), Times.Once);
    }

    // ------------------------------------------------------------- fil admin

    [Fact]
    public async Task GetThreadForAdminAsync_ReturnsMessagesAndMarksUserMessagesRead()
    {
        var expected = new List<DiscussionMessageDto>
        {
            new() { Id = 1, DiscussionId = DiscussionId, Message = "hello", IsFromAdmin = false }
        };
        _discussionRepository
            .Setup(_ => _.GetMessagesAsync(DiscussionId))
            .ReturnsAsync(expected);

        var messages = await _service.GetThreadForAdminAsync(DiscussionId);

        messages.Should().BeEquivalentTo(expected);
        _discussionRepository.Verify(_ => _.MarkMessagesAsReadAsync(DiscussionId, /* fromAdmin: */ false), Times.Once);
    }

    [Fact]
    public async Task PostAdminReplyAsync_PostsDirectlyOnTheGivenDiscussionWithoutLookingUpTheUser()
    {
        await _service.PostAdminReplyAsync(DiscussionId, "reply");

        _discussionRepository.Verify(_ => _.CreateMessageAsync(DiscussionId, "reply", /* isFromAdmin: */ true), Times.Once);
        _discussionRepository.Verify(_ => _.GetDiscussionByUserIdAsync(It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task GetAllDiscussionsAsync_DelegatesToTheRepositorySummaryQuery()
    {
        var expected = new List<DiscussionSummaryDto>
        {
            new() { DiscussionId = DiscussionId, UserId = UserId, UserLogin = "joueur1" }
        };
        _discussionRepository
            .Setup(_ => _.GetAllDiscussionSummariesAsync())
            .ReturnsAsync(expected);

        var discussions = await _service.GetAllDiscussionsAsync();

        discussions.Should().BeEquivalentTo(expected);
    }

    // ------------------------------------------------------------- pastille "a lire"

    [Fact]
    public async Task HasUnreadMessagesForUserAsync_DelegatesToTheMatchingRepositoryMethodOnly()
    {
        _discussionRepository
            .Setup(_ => _.HasUnreadMessagesForUserAsync(UserId))
            .ReturnsAsync(true);

        var result = await _service.HasUnreadMessagesForUserAsync(UserId);

        result.Should().BeTrue();
        _discussionRepository.Verify(_ => _.HasUnreadMessagesForAdminAsync(), Times.Never);
    }

    [Fact]
    public async Task HasUnreadMessagesForAdminAsync_DelegatesToTheMatchingRepositoryMethodOnly()
    {
        _discussionRepository
            .Setup(_ => _.HasUnreadMessagesForAdminAsync())
            .ReturnsAsync(true);

        var result = await _service.HasUnreadMessagesForAdminAsync();

        result.Should().BeTrue();
        _discussionRepository.Verify(_ => _.HasUnreadMessagesForUserAsync(It.IsAny<ulong>()), Times.Never);
    }
}
