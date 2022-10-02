using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Interfaces.Repositories
{
    public interface IClubRepository
    {
        Task<ulong> CreateClubAsync(ClubDto club);

        Task<ClubDto> GetClubAsync(ulong clubId);

        Task<IReadOnlyCollection<ClubDto>> GetClubsAsync();
    }
}
