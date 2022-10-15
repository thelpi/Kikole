using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories
{
    public interface IClubRepository
    {
        Task<ulong> CreateClubAsync(ClubDto club);

        Task UpdateClubAsync(ClubDto club);

        Task<ClubDto> GetClubAsync(ulong clubId);

        Task<IReadOnlyCollection<ClubDto>> GetClubsAsync();
    }
}
