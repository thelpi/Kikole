using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IDiscussionRepository
{
    Task<DiscussionDto?> GetDiscussionByUserIdAsync(ulong userId);

    Task<ulong> CreateDiscussionAsync(ulong userId);

    Task<IReadOnlyCollection<DiscussionMessageDto>> GetMessagesAsync(ulong discussionId);

    Task<ulong> CreateMessageAsync(ulong discussionId, string message, bool isFromAdmin);

    Task MarkMessagesAsReadAsync(ulong discussionId, bool fromAdmin);

    Task<bool> HasUnreadMessagesForUserAsync(ulong userId);

    Task<bool> HasUnreadMessagesForAdminAsync();

    Task<IReadOnlyCollection<DiscussionSummaryDto>> GetAllDiscussionSummariesAsync();
}
