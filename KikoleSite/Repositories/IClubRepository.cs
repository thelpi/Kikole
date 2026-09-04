using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IClubRepository
{
    Task<ulong> CreateClubAsync(ClubDto club);

    Task UpdateClubAsync(ClubDto club);

    Task<ClubDto?> GetClubAsync(ulong clubId);

    Task<IReadOnlyCollection<ClubDto>> GetClubsByIdsAsync(IReadOnlyCollection<ulong> clubIds);

    Task<IReadOnlyCollection<ClubDto>> GetClubsAsync();

    Task<IReadOnlyCollection<ClubTranslationDto>> GetClubTranslationsAsync();

    /// <summary>Remplace toutes les traductions d'un club par la liste fournie (le nombre d'alias est variable d'une modification à l'autre : un upsert par clé laisserait des lignes obsolètes).</summary>
    Task ReplaceClubTranslationsAsync(ulong clubId, IReadOnlyCollection<ClubTranslationDto> translations);
}
