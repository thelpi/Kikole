using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IStatisticRepository
{
    Task<IReadOnlyCollection<PlayersDistributionDto<ulong>>> GetPlayersDistributionByCountryAsync();

    Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByPositionAsync();

    Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByDecadeAsync();

    Task<IReadOnlyCollection<PlayersDistributionDto<ulong>>> GetPlayersDistributionByClubAsync();

    Task<IReadOnlyDictionary<(int y, int w), int>> GetWeeklyActiveUsersAsync(DateTime? startDate, DateTime? endDate);

    Task<IReadOnlyDictionary<(int y, int m), int>> GetMonthlyActiveUsersAsync(DateTime? startDate, DateTime? endDate);

    Task<IReadOnlyDictionary<DateTime, int>> GetDailyActiveUsersAsync(DateTime? startDate, DateTime? endDate);
}
