using System.Threading.Tasks;
using KikoleApi.Abstractions;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    public class ClubRepository : BaseRepository, IClubRepository
    {
        public ClubRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<ulong> CreateClubAsync(ClubDto club)
        {
            await ExecuteInsertAsync(
                    "clubs",
                    new[] { "name", "allowed_names", "creation_date" },
                    new object[] { club.Name, club.AllowedNames, Clock.Now })
                .ConfigureAwait(false);

            return await GetLastInsertedIdAsync().ConfigureAwait(false);
        }
    }
}
