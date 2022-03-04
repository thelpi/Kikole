using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IClubRepository
    {
        Task<ulong> CreateClubAsync(ClubDto club);

        Task<ClubDto> GetClubAsync(ulong clubId);
    }
}
