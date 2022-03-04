using System.Threading.Tasks;
using KikoleApi.Interfaces;
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
                    ("name", club.Name),
                    ("allowed_names", club.AllowedNames),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);

            return await GetLastInsertedIdAsync().ConfigureAwait(false);
        }

        public async Task<ClubDto> GetClubAsync(ulong clubId)
        {
            return await GetDtoAsync<ClubDto>(
                    "clubs",
                    ("id", clubId))
                .ConfigureAwait(false);
        }
    }
}
