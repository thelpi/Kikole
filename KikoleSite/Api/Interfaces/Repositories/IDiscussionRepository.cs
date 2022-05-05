using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
{
    public interface IDiscussionRepository
    {
        Task<ulong> CreateDiscussionAsync(DiscussionDto discussion);
    }
}
