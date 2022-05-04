using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
{
    public interface IClubRepository
    {
        Task<ulong> CreateClubAsync(ClubDto club);

        Task<ClubDto> GetClubAsync(ulong clubId);

        Task<IReadOnlyCollection<ClubDto>> GetClubsAsync();
    }
}
