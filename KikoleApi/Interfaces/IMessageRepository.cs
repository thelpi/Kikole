using System;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IMessageRepository
    {
        Task<MessageDto> GetMessageAsync(DateTime date);
    }
}
