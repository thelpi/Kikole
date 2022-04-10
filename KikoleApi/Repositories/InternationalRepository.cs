using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Interfaces.Repositories;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    public class InternationalRepository : BaseRepository, IInternationalRepository
    {
        public InternationalRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(ulong languageId)
        {
            return await ExecuteReaderAsync<CountryDto>(
                    "SELECT code, name FROM countries JOIN country_translations ON id = country_id WHERE language_id = @language_id",
                    new { language_id = languageId })
                .ConfigureAwait(false);
        }
    }
}
