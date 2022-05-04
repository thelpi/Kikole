using System;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models
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
