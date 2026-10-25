using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models;
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

    /// <summary>Noms (20 au plus) des kikolés, publiés ou non, dont la carrière contient le club.</summary>
    Task<IReadOnlyCollection<string>> GetPlayerNamesUsingClubAsync(ulong clubId);

    /// <summary>Propositions de type club visant ce club, sans suppression.</summary>
    Task<ClubUsage> GetClubUsageAsync(ulong clubId);

    /// <summary>
    /// Supprime le club s'il n'est dans la carrière d'aucun kikolé et visé par aucune proposition
    /// réussie ; ses propositions incorrectes gardent le nom du club (au lieu de son identifiant)
    /// pour que l'historique des joueurs reste lisible. Tout se fait dans une seule transaction.
    /// </summary>
    Task<ClubDeletionResult> DeleteClubAsync(ulong clubId);

    /// <summary>Remplace toutes les traductions d'un club par la liste fournie (le nombre d'alias est variable d'une modification à l'autre : un upsert par clé laisserait des lignes obsolètes).</summary>
    Task ReplaceClubTranslationsAsync(ulong clubId, IReadOnlyCollection<ClubTranslationDto> translations);
}
