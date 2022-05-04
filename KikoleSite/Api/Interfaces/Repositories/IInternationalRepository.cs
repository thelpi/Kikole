using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
{
    public interface IInternationalRepository
    {
        Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(ulong languageId);
    }
}
