using System.Collections.Generic;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models
{
    public class PlayersDistribution
    {
        public IReadOnlyCollection<PlayersDistributionDto<string>> CountriesDistribution { get; set; }
        public IReadOnlyCollection<PlayersDistributionDto<string>> ClubsDistribution { get; set; }
        public IReadOnlyCollection<PlayersDistributionDto<string>> DecadesDistribution { get; set; }
        public IReadOnlyCollection<PlayersDistributionDto<Positions>> PositionsDistribution { get; set; }
    }
}
