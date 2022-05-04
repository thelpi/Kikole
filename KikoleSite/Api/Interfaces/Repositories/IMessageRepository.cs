using System;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
{
    public interface IMessageRepository
    {
        Task<MessageDto> GetMessageAsync(DateTime date);
    }
}
