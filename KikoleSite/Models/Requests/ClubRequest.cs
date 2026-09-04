using System.Collections.Generic;
using KikoleSite.Helpers;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests;

public record ClubRequest
{
    public ulong Id { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<string> AllowedNames { get; init; }

    public ulong CountryId { get; init; }

    internal string? IsValid(IStringLocalizer resources)
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
            Name = Name,
            Id = Id,
            CountryId = CountryId
        };
    }
}
