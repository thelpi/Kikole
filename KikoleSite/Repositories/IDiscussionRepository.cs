using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories
{
    public interface IDiscussionRepository
    {
        Task<ulong> CreateDiscussionAsync(DiscussionDto discussion);

        Task<IReadOnlyCollection<DiscussionDto>> GetDiscussionsAsync();
    }
}
