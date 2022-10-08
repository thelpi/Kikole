using System;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories
{
    public interface IMessageRepository
    {
        Task<MessageDto> GetMessageAsync(DateTime date);

        Task InsertMessageAsync(MessageDto message);
    }
}
