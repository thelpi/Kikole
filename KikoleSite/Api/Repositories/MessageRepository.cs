using System;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Api.Repositories
{
    public class MessageRepository : BaseRepository, IMessageRepository
    {
        public MessageRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<MessageDto> GetMessageAsync(DateTime date)
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
                    })
                .ConfigureAwait(false);
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
}
