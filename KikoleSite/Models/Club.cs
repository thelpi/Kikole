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

    /// <summary>De 1 (défaut) à 3 : sert au tri de l'autocomplétion.</summary>
    public byte Importance { get; }

    /// <summary>Noms par langue, triés par priorité croissante : l'indice 0 est le nom canonique.</summary>
    public IReadOnlyDictionary<Languages, IReadOnlyList<string>> NamesByLanguage { get; }

    internal Club(ClubDto dto, IEnumerable<ClubTranslationDto> translations)
    {
        Id = dto.Id;
        Name = dto.Name;
        CountryId = dto.CountryId;
        Importance = dto.Importance;
        NamesByLanguage = translations
            .OrderBy(t => t.Priority)
            .GroupBy(t => (Languages)t.LanguageId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(t => t.Name).ToList());
    }

    public string GetCanonicalName(Languages language)
    {
        return NamesByLanguage[language][0];
    }

    /// <summary>Premier nom (par priorité, donc le canonique d'abord) contenant le terme ; null si aucun.</summary>
    public string? GetMatchingName(Languages language, string searchTerm)
    {
        var term = searchTerm.SanitizeForSearch();
        return term.Length > 0 && NamesByLanguage.TryGetValue(language, out var names)
            ? names.FirstOrDefault(n => n.SanitizeForSearch().Contains(term))
            : null;
    }
}
