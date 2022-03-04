using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IPlayerRepository
    {
        Task<ulong> CreatePlayerAsync(PlayerDto player);

        Task CreatePlayerClubsAsync(PlayerClubDto playerClub);
    }
}
