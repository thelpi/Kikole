using System;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces.Repositories
{
    public interface IMessageRepository
    {
        Task<MessageDto> GetMessageAsync(DateTime date);
    }
}
