using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Api.Repositories
{
    [ExcludeFromCodeCoverage]
    public class StatisticRepository : BaseRepository, IStatisticRepository
    {
        public StatisticRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<ulong>>> GetPlayersDistributionByClubAsync(ulong userId)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<ulong>>(
                    "SELECT l.club_id AS value, COUNT(*) AS count " +
                    "FROM players AS p " +
                    "   JOIN player_clubs AS l ON p.id = l.player_id " +
                    "WHERE p.proposal_date IS NOT NULL " +
                    UserPlayerLinkSql +
                    "GROUP BY l.club_id " +
                    "ORDER BY COUNT(*) DESC",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<ulong>>> GetPlayersDistributionByCountryAsync(ulong userId)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<ulong>>(
                    "SELECT country_id AS value, COUNT(*) AS count " +
                    "FROM players AS p " +
                    "WHERE proposal_date IS NOT NULL " +
                    UserPlayerLinkSql +
                    "GROUP BY country_id " +
                    "ORDER BY COUNT(*) DESC",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByDecadeAsync(ulong userId)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<int>>(
                    "SELECT CONCAT(SUBSTRING(year_of_birth, 1, 3), '0') AS value, COUNT(*) AS count " +
                    "FROM players AS p " +
                    "WHERE proposal_date IS NOT NULL " +
                    UserPlayerLinkSql +
                    "GROUP BY CONCAT(SUBSTRING(year_of_birth, 1, 3), '0') " +
                    "ORDER BY COUNT(*) DESC",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByPositionAsync(ulong userId)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<int>>(
                    "SELECT position_id AS value, COUNT(*) AS count " +
                    "FROM players AS p " +
                    "WHERE proposal_date IS NOT NULL " +
                    UserPlayerLinkSql +
                    "GROUP BY position_id " +
                    "ORDER BY COUNT(*) DESC",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyDictionary<int, int>> GetActivityDatasAsync()
        {
            var rawDatas = await ExecuteReaderAsync<(int, int)>(
                    "SELECT WEEK(creation_date), COUNT(DISTINCT user_id) " +
                    "FROM proposals " +
                    "GROUP BY WEEK(creation_date) " +
                    "ORDER BY WEEK(creation_date)",
                    null)
                .ConfigureAwait(false);

            return rawDatas.ToDictionary(_ => _.Item1, _ => _.Item2);
        }

        private static readonly string UserPlayerLinkSql = 
            "AND (" +
            "   EXISTS (" +
            "       SELECT 1 FROM users AS u " +
            "       WHERE u.id = @userId " +
            "           AND u.user_type_id = " + (ulong)UserTypes.Administrator +
            "   ) OR EXISTS (" +
            "       SELECT 1 FROM leaders AS ld " +
            "       WHERE ld.proposal_date = p.proposal_date " +
            "           AND ld.user_id = @userId " +
            "   ) OR p.creation_user_id = @userId " +
            ") ";
    }
}
