using System;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Interfaces.Repositories
{
    public interface IMessageRepository
    {
        Task<MessageDto> GetMessageAsync(DateTime date);

        Task InsertMessageAsync(MessageDto message);
    }
}
