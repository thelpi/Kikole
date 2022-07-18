using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
{
    public interface IStatisticRepository
    {
        Task<IReadOnlyCollection<PlayersDistributionDto<string>>> GetPlayersDistributionByCountryAsync(ulong userId, ulong languageId);

        Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByPositionAsync(ulong userId);

        Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByDecadeAsync(ulong userId);

        Task<IReadOnlyCollection<PlayersDistributionDto<string>>> GetPlayersDistributionByClubAsync(ulong userId);
    }
}
