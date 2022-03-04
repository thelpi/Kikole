using System.Threading.Tasks;
using KikoleApi.Domain.Models.Dtos;

namespace KikoleApi.Repositories
{
    public interface IPlayerRepository
    {
        Task<long> CreatePlayerAsync(PlayerDto player);

        Task CreatePlayerNamesAsync(PlayerNameDto playerName);

        Task CreatePlayerClubsAsync(PlayerClubDto playerClub);
    }
}
