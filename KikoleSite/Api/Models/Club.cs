using System.Collections.Generic;
using KikoleSite.Api.Helpers;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Models
{
    public class Club
    {
        public ulong Id { get; }

        public string Name { get; }

        public IReadOnlyList<string> AllowedNames { get; }

        internal Club(ClubDto dto)
        {
            Name = dto.Name;
            AllowedNames = dto.AllowedNames.Disjoin();
            Id = dto.Id;
        }
    }
}
