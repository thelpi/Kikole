using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories
{
    public class InternationalRepository : BaseRepository, IInternationalRepository
    {
        public InternationalRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(ulong languageId)
        {
            return await ExecuteReaderAsync<CountryDto>(
                    "SELECT code, name " +
                    "FROM countries " +
                    "JOIN country_translations ON id = country_id " +
                    "WHERE language_id = @language_id",
                    new { language_id = languageId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ContinentDto>> GetContinentsAsync(ulong languageId)
        {
            return await ExecuteReaderAsync<ContinentDto>(
                    "SELECT id, ct.name " +
                    "FROM continents " +
                    "JOIN continent_translations AS ct ON id = continent_id " +
                    "WHERE language_id = @language_id",
                    new { language_id = languageId })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<FederationDto>> GetFederationsAsync(ulong languageId)
        {
            return await ExecuteReaderAsync<FederationDto>(
                    "SELECT code, name " +
                    "FROM federations " +
                    "JOIN federation_translations ON id = federation_id " +
                    "WHERE language_id = @language_id",
                    new { language_id = languageId })
                .ConfigureAwait(false);
        }
    }
}
