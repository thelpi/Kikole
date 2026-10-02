using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories;

public class StatisticRepository : BaseRepository, IStatisticRepository
{
    public StatisticRepository(IConfiguration configuration, IClock clock)
        : base(configuration, clock)
    { }

    public async Task<IReadOnlyCollection<PlayersDistributionDto<ulong>>> GetPlayersDistributionByClubAsync()
    {
        return await ExecuteReaderAsync<PlayersDistributionDto<ulong>>(
                "SELECT l.club_id AS value, COUNT(*) AS count " +
                "FROM players AS p " +
                "   JOIN player_clubs AS l ON p.id = l.player_id " +
                "WHERE p.publication_date IS NOT NULL " +
                "GROUP BY l.club_id " +
                "ORDER BY COUNT(*) DESC",
                null);
    }

    public async Task<IReadOnlyCollection<PlayersDistributionDto<ulong>>> GetPlayersDistributionByCountryAsync()
    {
        return await ExecuteReaderAsync<PlayersDistributionDto<ulong>>(
                "SELECT country_id AS value, COUNT(*) AS count " +
                "FROM players " +
                "WHERE publication_date IS NOT NULL " +
                "GROUP BY country_id " +
                "ORDER BY COUNT(*) DESC",
                null);
    }

    public async Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByDecadeAsync()
    {
        return await ExecuteReaderAsync<PlayersDistributionDto<int>>(
                "SELECT CONCAT(SUBSTRING(year_of_birth, 1, 3), '0') AS value, COUNT(*) AS count " +
                "FROM players " +
                "WHERE publication_date IS NOT NULL " +
                "GROUP BY CONCAT(SUBSTRING(year_of_birth, 1, 3), '0') " +
                "ORDER BY COUNT(*) DESC",
                null);
    }

    public async Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByPositionAsync()
    {
        return await ExecuteReaderAsync<PlayersDistributionDto<int>>(
                "SELECT position_id AS value, COUNT(*) AS count " +
                "FROM players " +
                "WHERE publication_date IS NOT NULL " +
                "GROUP BY position_id " +
                "ORDER BY COUNT(*) DESC",
                null);
    }

    public async Task<IReadOnlyDictionary<(int y, int w), int>> GetWeeklyActiveUsersAsync(DateTime? startDate, DateTime? endDate)
    {
        var rawDatas = await ExecuteReaderAsync<(int y, int w, int c)>(
                "SELECT YEAR(creation_date), WEEK(creation_date), COUNT(DISTINCT user_id) " +
                "FROM proposals " +
                "WHERE (@startDate IS NULL OR creation_date >= @startDate) " +
                "   AND (@endDate IS NULL OR creation_date < @endDate) " +
                $"   AND user_id IN ({SubSqlValidUsers}) " +
                "GROUP BY YEAR(creation_date), WEEK(creation_date) " +
                "ORDER BY YEAR(creation_date), WEEK(creation_date)",
                new { startDate, endDate });

        return rawDatas.ToDictionary(_ => (_.y, _.w), _ => _.c);
    }

    public async Task<IReadOnlyDictionary<(int y, int m), int>> GetMonthlyActiveUsersAsync(DateTime? startDate, DateTime? endDate)
    {
        var rawDatas = await ExecuteReaderAsync<(int y, int m, int c)>(
                "SELECT YEAR(creation_date), MONTH(creation_date), COUNT(DISTINCT user_id) " +
                "FROM proposals " +
                "WHERE (@startDate IS NULL OR creation_date >= @startDate) " +
                "   AND (@endDate IS NULL OR creation_date < @endDate) " +
                $"   AND user_id IN ({SubSqlValidUsers}) " +
                "GROUP BY YEAR(creation_date), MONTH(creation_date) " +
                "ORDER BY YEAR(creation_date), MONTH(creation_date)",
                new { startDate, endDate });

        return rawDatas.ToDictionary(_ => (_.y, _.m), _ => _.c);
    }

    public async Task<IReadOnlyDictionary<DateTime, int>> GetDailyActiveUsersAsync(DateTime? startDate, DateTime? endDate)
    {
        var rawDatas = await ExecuteReaderAsync<(DateTime d, int c)>(
                "SELECT DATE(creation_date), COUNT(DISTINCT user_id) " +
                "FROM proposals " +
                "WHERE (@startDate IS NULL OR creation_date >= @startDate) " +
                "   AND (@endDate IS NULL OR creation_date < @endDate) " +
                $"   AND user_id IN ({SubSqlValidUsers}) " +
                "GROUP BY DATE(creation_date) " +
                "ORDER BY DATE(creation_date)",
                new { startDate, endDate });

        return rawDatas.ToDictionary(_ => _.d, _ => _.c);
    }
}
