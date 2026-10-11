using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IMessageRepository
{
    Task<MessageDto?> GetMessageAsync(DateTime date);

    Task InsertMessageAsync(MessageDto message);

    Task<IReadOnlyList<MessageDto>> GetMessagesAsync();

    Task<MessageDto?> GetMessageByIdAsync(ulong id);

    Task UpdateMessageAsync(MessageDto message);

    Task DeleteMessageAsync(ulong id);
}
