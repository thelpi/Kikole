using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories
{
    public class ClubRepository : BaseRepository, IClubRepository
    {
        public ClubRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<ulong> CreateClubAsync(ClubDto club)
        {
            return await ExecuteInsertAsync(
                    "clubs",
                    ("name", club.Name),
                    ("allowed_names", club.AllowedNames),
                    ("creation_date", Clock.Now))
                .ConfigureAwait(false);
        }

        public async Task<ClubDto> GetClubAsync(ulong clubId)
        {
            return await GetDtoAsync<ClubDto>(
                    "clubs",
                    ("id", clubId))
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ClubDto>> GetClubsAsync()
        {
            return await GetDtosAsync<ClubDto>(
                    "clubs")
                .ConfigureAwait(false);
        }
    }
}
