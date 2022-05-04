using System.Collections.Generic;
using KikoleSite.Api.Helpers;
using KikoleSite.Api.Models.Dtos;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Api.Models.Requests
{
    public class ClubRequest
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }

        internal string IsValid(IStringLocalizer resources)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return resources["InvalidName"];

            if (!AllowedNames.IsValid())
                return resources["InvalidAllowedNames"];

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
