using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces.Repositories
{
    public interface IInternationalRepository
    {
        Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(ulong languageId);
    }
}
