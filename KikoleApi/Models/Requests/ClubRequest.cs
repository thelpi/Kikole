using System.Collections.Generic;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models.Requests
{
    public class ClubRequest
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }

        internal string IsValid(TextResources resources)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return resources.InvalidName;

            if (!AllowedNames.IsValid())
                return resources.InvalidAllowedNames;

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
