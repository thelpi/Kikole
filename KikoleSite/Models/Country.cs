﻿using System;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
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
