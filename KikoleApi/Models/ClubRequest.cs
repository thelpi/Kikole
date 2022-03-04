using System.Collections.Generic;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ClubRequest
    {
        public string Name { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }

        internal string IsValid()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "Invalid name";

            if (!AllowedNames.IsValid())
                return "Invalid allowed names";

            return null;
        }

        internal ClubDto ToDto()
        {
            return new ClubDto
            {
                AllowedNames = AllowedNames.SanitizeJoin(Name),
                Name = Name
            };
        }
    }
}
