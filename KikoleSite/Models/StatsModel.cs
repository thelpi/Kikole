using System.Collections.Generic;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Models
{
    public class StatsModel
    {
        public IReadOnlyCollection<PlayersDistributionDto<object>> DistributionCountries { get; set; }
        public IReadOnlyCollection<PlayersDistributionDto<object>> DistributionClubs { get; set; }
        public IReadOnlyCollection<PlayersDistributionDto<object>> DistributionDecades { get; set; }
        public IReadOnlyCollection<PlayersDistributionDto<object>> DistributionPositions { get; set; }
    }
}
