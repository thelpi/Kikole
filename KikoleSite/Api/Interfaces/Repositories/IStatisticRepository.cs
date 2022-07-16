using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
{
    public interface IStatisticRepository
    {
        Task<IReadOnlyCollection<PlayersDistributionDto<string>>> GetPlayersDistributionByCountryAsync(ulong languageId, DateTime? minDateInc = null, DateTime? maxDateExc = null);

        Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByPositionAsync(DateTime? minDateInc = null, DateTime? maxDateExc = null);

        Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByDecadeAsync(DateTime? minDateInc = null, DateTime? maxDateExc = null);

        Task<IReadOnlyCollection<PlayersDistributionDto<string>>> GetPlayersDistributionByClubAsync(DateTime? minDateInc = null, DateTime? maxDateExc = null);
    }
}
