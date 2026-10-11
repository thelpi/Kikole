using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories;

public class MessageRepository : BaseRepository, IMessageRepository
{
    public MessageRepository(IConfiguration configuration, IClock clock)
        : base(configuration, clock)
    { }

    public async Task<MessageDto?> GetMessageAsync(DateTime date)
    {
        return await ExecuteScalarAsync<MessageDto>(
                "SELECT * FROM messages " +
                "WHERE (display_from IS NULL or display_from <= @date) " +
                "AND (display_to IS NULL or display_to > @date) " +
                "ORDER BY ISNULL(display_from) ASC, display_from DESC, " +
                "ISNULL(display_to) ASC, display_from ASC",
                new
                {
                    date
                });
    }

    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync()
    {
        return await ExecuteReaderAsync<MessageDto>(
                "SELECT * FROM messages ORDER BY creation_date DESC, id DESC",
                null);
    }

    public async Task<MessageDto?> GetMessageByIdAsync(ulong id)
    {
        return await GetDtoAsync<MessageDto>("messages", ("id", id));
    }

    public async Task UpdateMessageAsync(MessageDto message)
    {
        await ExecuteNonQueryAsync(
                "UPDATE messages " +
                "SET message = @message, display_from = @displayFrom, display_to = @displayTo " +
                "WHERE id = @id",
                new
                {
                    id = message.Id,
                    message = message.Message,
                    displayFrom = message.DisplayFrom,
                    displayTo = message.DisplayTo
                });
    }

    public async Task DeleteMessageAsync(ulong id)
    {
        await ExecuteNonQueryAsync("DELETE FROM messages WHERE id = @id", new { id });
    }

    public async Task InsertMessageAsync(MessageDto message)
    {
        await ExecuteInsertAsync("messages",
            ("display_from", message.DisplayFrom),
            ("display_to", message.DisplayTo),
            ("message", message.Message),
            ("creation_date", message.CreationDate));
    }
}
