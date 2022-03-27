using System.Collections.Generic;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
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
