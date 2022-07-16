using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Api.Repositories
{
    [ExcludeFromCodeCoverage]
    public class StatisticRepository : BaseRepository, IStatisticRepository
    {
        public StatisticRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<string>>> GetPlayersDistributionByClubAsync(DateTime? minDateInc = null, DateTime? maxDateExc = null)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<string>>(
                    "SELECT c.name AS value, COUNT(*) AS count " +
                    "FROM players AS p " +
                    "   JOIN player_clubs AS l ON p.id = l.player_id " +
                    "   JOIN clubs AS c ON l.club_id = c.id " +
                    "WHERE p.proposal_date IS NOT NULL " +
                    "   AND (@minDate IS NULL OR p.proposal_date >= DATE(@minDate)) " +
                    "   AND (@maxDate IS NULL OR p.proposal_date < DATE(@maxDate)) " +
                    "GROUP BY c.name " +
                    "ORDER BY COUNT(*) DESC",
                    new
                    {
                        minDate = minDateInc,
                        maxDate = maxDateExc
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<string>>> GetPlayersDistributionByCountryAsync(ulong languageId, DateTime? minDateInc = null, DateTime? maxDateExc = null)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<string>>(
                    "SELECT t.name AS value, COUNT(*) AS count " +
                    "FROM players AS p " +
                    "   JOIN countries AS c ON p.country_id = c.id " +
                    "   JOIN country_translations AS t ON c.id = t.country_id " +
                    "WHERE t.language_id = @languageId " +
                    "   AND p.proposal_date IS NOT NULL " +
                    "   AND (@minDate IS NULL OR p.proposal_date >= DATE(@minDate)) " +
                    "   AND (@maxDate IS NULL OR p.proposal_date < DATE(@maxDate)) " +
                    "GROUP BY t.name " +
                    "ORDER BY COUNT(*) DESC",
                    new
                    {
                        minDate = minDateInc,
                        maxDate = maxDateExc,
                        languageId
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByDecadeAsync(DateTime? minDateInc = null, DateTime? maxDateExc = null)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<int>>(
                    "SELECT SUBSTRING(year_of_birth, 1, 3) AS value, COUNT(*) AS count " +
                    "FROM players " +
                    "WHERE proposal_date IS NOT NULL " +
                    "   AND (@minDate IS NULL OR proposal_date >= DATE(@minDate)) " +
                    "   AND (@maxDate IS NULL OR proposal_date < DATE(@maxDate)) " +
                    "GROUP BY SUBSTRING(year_of_birth, 1, 3) " +
                    "ORDER BY COUNT(*) DESC",
                    new
                    {
                        minDate = minDateInc,
                        maxDate = maxDateExc
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayersDistributionDto<int>>> GetPlayersDistributionByPositionAsync(DateTime? minDateInc = null, DateTime? maxDateExc = null)
        {
            return await ExecuteReaderAsync<PlayersDistributionDto<int>>(
                    "SELECT position_id AS value, COUNT(*) AS count " +
                    "FROM players " +
                    "WHERE proposal_date IS NOT NULL " +
                    "   AND (@minDate IS NULL OR proposal_date >= DATE(@minDate)) " +
                    "   AND (@maxDate IS NULL OR proposal_date < DATE(@maxDate)) " +
                    "GROUP BY position_id " +
                    "ORDER BY COUNT(*) DESC",
                    new
                    {
                        minDate = minDateInc,
                        maxDate = maxDateExc
                    })
                .ConfigureAwait(false);
        }
    }
}
