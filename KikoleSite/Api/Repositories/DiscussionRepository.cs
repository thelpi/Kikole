using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Api.Repositories
{
    [ExcludeFromCodeCoverage]
    public class DiscussionRepository : BaseRepository, IDiscussionRepository
    {
        public DiscussionRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<ulong> CreateDiscussionAsync(DiscussionDto discussion)
        {
            return await ExecuteInsertAsync("discussions",
                ("email", discussion.Email),
                ("message", discussion.Message),
                ("creation_date", Clock.Now),
                ("user_id", discussion.UserId));
        }
    }
}
