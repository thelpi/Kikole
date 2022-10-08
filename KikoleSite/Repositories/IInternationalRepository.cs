using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories
{
    public interface IInternationalRepository
    {
        Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(ulong languageId);
    }
}
