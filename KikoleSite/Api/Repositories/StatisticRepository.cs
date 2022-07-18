using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public async Task<IReadOnlyCollection<PlayersDistributionDto<string>>> GetPlayersDistributionByClubAsync(ulong userId)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<string>>(
                    "SELECT c.name AS value, COUNT(*) AS count " +
                    "FROM players AS p " +
                    "   JOIN player_clubs AS l ON p.id = l.player_id " +
                    "   JOIN clubs AS c ON l.club_id = c.id " +
                    "WHERE p.proposal_date IS NOT NULL " +
                    UserPlayerLinkSql +
                    "GROUP BY c.name " +
                    "ORDER BY COUNT(*) DESC",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<string>>> GetPlayersDistributionByCountryAsync(ulong userId, ulong languageId)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<string>>(
                    "SELECT t.name AS value, COUNT(*) AS count " +
                    "FROM players AS p " +
                    "   JOIN countries AS c ON p.country_id = c.id " +
                    "   JOIN country_translations AS t ON c.id = t.country_id " +
                    "WHERE t.language_id = @languageId " +
                    "   AND p.proposal_date IS NOT NULL " +
                    UserPlayerLinkSql +
                    "GROUP BY t.name " +
                    "ORDER BY COUNT(*) DESC",
                    new { userId, languageId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByDecadeAsync(ulong userId)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<int>>(
                    "SELECT SUBSTRING(year_of_birth, 1, 3) AS value, COUNT(*) AS count " +
                    "FROM players AS p " +
                    "WHERE proposal_date IS NOT NULL " +
                    UserPlayerLinkSql +
                    "GROUP BY SUBSTRING(year_of_birth, 1, 3) " +
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
