using System;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
{
    public class Federation
    {
        public Federations Code { get; }

        public string Name { get; }

        internal Federation(FederationDto dto)
        {
            Code = Enum.Parse<Federations>(dto.Code);
            Name = dto.Name;
        }
    }
}
