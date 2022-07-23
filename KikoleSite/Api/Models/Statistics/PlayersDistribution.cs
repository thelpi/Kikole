using System.Collections.Generic;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models.Statistics
{
    public class PlayersDistribution
    {
        public int TotalPlayersCount { get; set; }
        public IReadOnlyCollection<PlayersDistributionItem<Country>> CountriesDistribution { get; set; }
        public IReadOnlyCollection<PlayersDistributionItem<Club>> ClubsDistribution { get; set; }
        public IReadOnlyCollection<PlayersDistributionItem<int>> DecadesDistribution { get; set; }
        public IReadOnlyCollection<PlayersDistributionItem<Positions>> PositionsDistribution { get; set; }
    }
}
