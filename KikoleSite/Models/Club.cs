using System.Collections.Generic;
using KikoleSite.Helpers;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Models
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
