using System;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models
{
    public class Country
    {
        public Countries Code { get; }

        public string Name { get; }

        internal Country(CountryDto dto)
        {
            Code = Enum.Parse<Countries>(dto.Code);
            Name = dto.Name;
        }
    }
}
