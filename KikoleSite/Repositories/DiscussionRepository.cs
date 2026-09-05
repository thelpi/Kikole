using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories;

public class DiscussionRepository : BaseRepository, IDiscussionRepository
{
    public DiscussionRepository(IConfiguration configuration, IClock clock)
        : base(configuration, clock)
    { }

    public async Task<DiscussionDto?> GetDiscussionByUserIdAsync(ulong userId)
    {
        return await GetDtoAsync<DiscussionDto>("discussions", ("user_id", userId));
    }

    public async Task<ulong> CreateDiscussionAsync(ulong userId)
    {
        return await ExecuteInsertAsync(
                "discussions",
                ("user_id", userId),
                ("creation_date", Clock.Now));
    }

    public async Task<IReadOnlyCollection<DiscussionMessageDto>> GetMessagesAsync(ulong discussionId)
    {
        return await ExecuteReaderAsync<DiscussionMessageDto>(
                "SELECT * FROM discussion_messages " +
                "WHERE discussion_id = @discussionId " +
                "ORDER BY creation_date",
                new { discussionId });
    }

    public async Task<ulong> CreateMessageAsync(ulong discussionId, string message, bool isFromAdmin)
    {
        return await ExecuteInsertAsync(
                "discussion_messages",
                ("discussion_id", discussionId),
                ("message", message),
                ("creation_date", Clock.Now),
                ("is_from_admin", isFromAdmin),
                ("is_read", false));
    }

    public async Task MarkMessagesAsReadAsync(ulong discussionId, bool fromAdmin)
    {
        await ExecuteNonQueryAsync(
                "UPDATE discussion_messages " +
                "SET is_read = 1 " +
                "WHERE discussion_id = @discussionId " +
                "   AND is_from_admin = @fromAdmin " +
                "   AND is_read = 0",
                new { discussionId, fromAdmin });
    }

    public async Task<bool> HasUnreadMessagesForUserAsync(ulong userId)
    {
        var count = await ExecuteScalarAsync(
                "SELECT COUNT(*) FROM discussion_messages dm " +
                "JOIN discussions d ON d.id = dm.discussion_id " +
                "WHERE d.user_id = @userId AND dm.is_from_admin = 1 AND dm.is_read = 0",
                new { userId }, 0);

        return count > 0;
    }

    public async Task<bool> HasUnreadMessagesForAdminAsync()
    {
        var count = await ExecuteScalarAsync(
                "SELECT COUNT(*) FROM discussion_messages " +
                "WHERE is_from_admin = 0 AND is_read = 0",
                null, 0);

        return count > 0;
    }

    public async Task<IReadOnlyCollection<DiscussionSummaryDto>> GetAllDiscussionSummariesAsync()
    {
        return await ExecuteReaderAsync<DiscussionSummaryDto>(
                "SELECT d.id AS DiscussionId, d.user_id AS UserId, u.login AS UserLogin, " +
                "   MAX(dm.creation_date) AS LastMessageDate, " +
                "   MAX(CASE WHEN dm.is_from_admin = 0 AND dm.is_read = 0 THEN 1 ELSE 0 END) AS HasUnreadFromUser " +
                "FROM discussions d " +
                "JOIN users u ON u.id = d.user_id " +
                "LEFT JOIN discussion_messages dm ON dm.discussion_id = d.id " +
                "GROUP BY d.id, d.user_id, u.login " +
                "ORDER BY LastMessageDate DESC",
                null);
    }
}
