using System.Collections.Generic;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Models
{
    public class StatsModel
    {
        public IReadOnlyCollection<PlayersDistributionDto<string>> DistributionCountries { get; set; }
        public IReadOnlyCollection<PlayersDistributionDto<string>> DistributionClubs { get; set; }
        public IReadOnlyCollection<PlayersDistributionDto<string>> DistributionDecades { get; set; }
        public IReadOnlyCollection<PlayersDistributionDto<Positions>> DistributionPositions { get; set; }
    }
}
