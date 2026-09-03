using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models;

/// <summary>
/// Le barème du jeu et le calcul qui le consomme : les deux sont statiques et sans
/// dépendance (aucune I/O), donc rassemblés au même endroit plutôt que scindés entre une
/// donnée pure (l'ancien <c>ProposalChart</c>) et un calcul qui vivait dans
/// <c>ProposalService</c> par accident d'implémentation. C'est justement parce que ce
/// calcul n'avait pas sa place dans un service qu'un autre service pouvait le contourner
/// par un appel statique direct.
/// </summary>
public static class ScoreCalculator
{
    public static readonly int BasePoints = 1000;
    public static readonly int SubmissionPoints = 1000;

    public static readonly IReadOnlyDictionary<ProposalTypes, (int points, bool isRate)> ProposalTypesCost
        = new Dictionary<ProposalTypes, (int, bool)>
        {
            { ProposalTypes.Club, (50, false) },
            { ProposalTypes.Country, (25, false) },
            { ProposalTypes.Name, (400, false) },
            { ProposalTypes.Position, (75, false) },
            { ProposalTypes.Year, (25, false) },
            { ProposalTypes.Clue, (50, true) },
            { ProposalTypes.Leaderboard, (25, false) },
            { ProposalTypes.Continent, (100, false) }
        };

    internal static List<ProposalResponse> GetProposalResponsesWithPoints(
        IEnumerable<ProposalDto> proposalDtos,
        PlayerFullDto player,
        out int points,
        IStringLocalizer<Translations> resources)
    {
        var totalPoints = BasePoints;
        var proposals = proposalDtos
            .OrderBy(pDto => pDto.CreationDate)
            .Select(pDto =>
            {
                var pr = new ProposalResponse(pDto, player, resources)
                    .WithTotalPoints(totalPoints, false);
                totalPoints = pr.TotalPoints;
                return pr;
            })
            .ToList();

        points = totalPoints;
        return proposals;
    }
}
