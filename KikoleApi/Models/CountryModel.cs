using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class CountryModel
    {
        public Country Code { get; }

        public string Name { get; }

        internal CountryModel(CountryDto dto)
        {
            Code = Enum.Parse<Country>(dto.Code);
            Name = dto.Name;
        }
    }
}
