using System;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
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
    }
}
