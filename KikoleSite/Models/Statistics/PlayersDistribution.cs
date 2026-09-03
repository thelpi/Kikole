using System.Collections.Generic;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models.Statistics;

public class PlayersDistribution
{
    public int TotalPlayersCount { get; set; }
    public required IReadOnlyCollection<PlayersDistributionItem<Country>> CountriesDistribution { get; set; }
    public required IReadOnlyCollection<PlayersDistributionItem<Club>> ClubsDistribution { get; set; }
    public required IReadOnlyCollection<PlayersDistributionItem<int>> DecadesDistribution { get; set; }
    public required IReadOnlyCollection<PlayersDistributionItem<Positions>> PositionsDistribution { get; set; }
}
