using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces.Repositories
{
    public interface IClubRepository
    {
        Task<ulong> CreateClubAsync(ClubDto club);

        Task<ClubDto> GetClubAsync(ulong clubId);

        Task<IReadOnlyCollection<ClubDto>> GetClubsAsync();
    }
}
