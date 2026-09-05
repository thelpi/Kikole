using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Services;

/// <summary>
/// Discussion service interface.
/// </summary>
public interface IDiscussionService
{
    /// <summary>
    /// Gets a user's own discussion thread, creating it lazily. Marks the admin's
    /// messages as read as a side effect.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>Messages ordered chronologically; empty if the user never wrote yet.</returns>
    Task<IReadOnlyCollection<DiscussionMessageDto>> GetOwnThreadAsync(ulong userId);

    /// <summary>
    /// Posts a message on a user's own discussion thread, creating it if this is the
    /// user's first message.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="message">Message content.</param>
    /// <returns>Nothing.</returns>
    Task PostUserMessageAsync(ulong userId, string message);

    /// <summary>
    /// Gets a discussion thread for the administrator. Marks the user's messages as
    /// read as a side effect.
    /// </summary>
    /// <param name="discussionId">Discussion identifier.</param>
    /// <returns>Messages ordered chronologically.</returns>
    Task<IReadOnlyCollection<DiscussionMessageDto>> GetThreadForAdminAsync(ulong discussionId);

    /// <summary>
    /// Posts an administrator reply on an existing discussion thread.
    /// </summary>
    /// <param name="discussionId">Discussion identifier.</param>
    /// <param name="message">Message content.</param>
    /// <returns>Nothing.</returns>
    Task PostAdminReplyAsync(ulong discussionId, string message);

    /// <summary>
    /// Gets every discussion thread, one row per user, for the administrator inbox.
    /// </summary>
    /// <returns>Collection of <see cref="DiscussionSummaryDto"/>.</returns>
    Task<IReadOnlyCollection<DiscussionSummaryDto>> GetAllDiscussionsAsync();

    /// <summary>
    /// Checks if a user's own discussion has at least one unread admin message.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns><c>True</c> if a message is waiting to be read.</returns>
    Task<bool> HasUnreadMessagesForUserAsync(ulong userId);

    /// <summary>
    /// Checks if any discussion has at least one unread user message (administrator
    /// inbox, every discussion combined).
    /// </summary>
    /// <returns><c>True</c> if a message is waiting to be read.</returns>
    Task<bool> HasUnreadMessagesForAdminAsync();
}
