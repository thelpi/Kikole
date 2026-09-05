using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using KikoleSite.Repositories;

namespace KikoleSite.Services;

public class DiscussionService : IDiscussionService
{
    private readonly IDiscussionRepository _discussionRepository;

    public DiscussionService(IDiscussionRepository discussionRepository)
    {
        _discussionRepository = discussionRepository;
    }

    public async Task<IReadOnlyCollection<DiscussionMessageDto>> GetOwnThreadAsync(ulong userId)
    {
        var discussion = await _discussionRepository.GetDiscussionByUserIdAsync(userId);
        if (discussion == null)
            return [];

        var messages = await _discussionRepository.GetMessagesAsync(discussion.Id);
        await _discussionRepository.MarkMessagesAsReadAsync(discussion.Id, fromAdmin: true);
        return messages;
    }

    public async Task PostUserMessageAsync(ulong userId, string message)
    {
        var discussion = await _discussionRepository.GetDiscussionByUserIdAsync(userId);
        var discussionId = discussion?.Id
            ?? await _discussionRepository.CreateDiscussionAsync(userId);

        await _discussionRepository.CreateMessageAsync(discussionId, message, isFromAdmin: false);
    }

    public async Task<IReadOnlyCollection<DiscussionMessageDto>> GetThreadForAdminAsync(ulong discussionId)
    {
        var messages = await _discussionRepository.GetMessagesAsync(discussionId);
        await _discussionRepository.MarkMessagesAsReadAsync(discussionId, fromAdmin: false);
        return messages;
    }

    public async Task PostAdminReplyAsync(ulong discussionId, string message)
    {
        await _discussionRepository.CreateMessageAsync(discussionId, message, isFromAdmin: true);
    }

    public async Task<IReadOnlyCollection<DiscussionSummaryDto>> GetAllDiscussionsAsync()
    {
        return await _discussionRepository.GetAllDiscussionSummariesAsync();
    }

    public async Task<bool> HasUnreadMessagesForUserAsync(ulong userId)
    {
        return await _discussionRepository.HasUnreadMessagesForUserAsync(userId);
    }

    public async Task<bool> HasUnreadMessagesForAdminAsync()
    {
        return await _discussionRepository.HasUnreadMessagesForAdminAsync();
    }
}
