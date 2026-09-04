using System.Collections.Generic;
using System.Linq;
using KikoleSite.Helpers;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models;

public class Club
{
    public ulong Id { get; }

    public string Name { get; }

    public ulong CountryId { get; }

    /// <summary>Noms par langue, triés par priorité croissante : l'indice 0 est le nom canonique.</summary>
    public IReadOnlyDictionary<Languages, IReadOnlyList<string>> NamesByLanguage { get; }

    internal Club(ClubDto dto, IEnumerable<ClubTranslationDto> translations)
    {
        Id = dto.Id;
        Name = dto.Name;
        CountryId = dto.CountryId;
        NamesByLanguage = translations
            .OrderBy(t => t.Priority)
            .GroupBy(t => (Languages)t.LanguageId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(t => t.Name).ToList());
    }

    public string GetCanonicalName(Languages language)
    {
        return NamesByLanguage[language][0];
    }

    public bool MatchesSearch(Languages language, string sanitizedPrefix)
    {
        return NamesByLanguage.TryGetValue(language, out var names)
            && names.Any(n => n.Sanitize().Contains(sanitizedPrefix));
    }
}
