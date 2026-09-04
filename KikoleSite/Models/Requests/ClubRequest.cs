using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests;

public record ClubRequest
{
    public ulong Id { get; init; }

    /// <summary>Noms par langue, triés par priorité croissante : l'indice 0 est le nom canonique (obligatoire pour FR et EN).</summary>
    public required IReadOnlyDictionary<Languages, IReadOnlyList<string>> NamesByLanguage { get; init; }

    public ulong CountryId { get; init; }

    internal string? IsValid(IStringLocalizer resources)
    {
        foreach (var language in new[] { Languages.fr, Languages.en })
        {
            if (!NamesByLanguage.TryGetValue(language, out var names)
                || names.Count == 0
                || string.IsNullOrWhiteSpace(names[0]))
                return resources["InvalidName"];
        }

        return null;
    }

    internal ClubDto ToDto()
    {
        return new ClubDto
        {
            Name = NamesByLanguage[Languages.fr][0],
            Id = Id,
            CountryId = CountryId
        };
    }

    internal IReadOnlyCollection<ClubTranslationDto> ToTranslationDtos(ulong clubId)
    {
        return NamesByLanguage
            .SelectMany(kvp => kvp.Value.Select((name, index) => new ClubTranslationDto
            {
                ClubId = clubId,
                LanguageId = (ulong)kvp.Key,
                Priority = (byte)index,
                Name = name
            }))
            .ToList();
    }
}
