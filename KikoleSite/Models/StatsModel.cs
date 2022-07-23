using System.Collections.Generic;

namespace KikoleSite.Models
{
    public class StatsModel
    {
        public IReadOnlyCollection<(int rank, int count, string value, decimal rate)> DistributionCountries { get; set; }
        public IReadOnlyCollection<(int rank, int count, string value, decimal rate)> DistributionClubs { get; set; }
        public IReadOnlyCollection<(int rank, int count, string value, decimal rate)> DistributionDecades { get; set; }
        public IReadOnlyCollection<(int rank, int count, string value, decimal rate)> DistributionPositions { get; set; }
    }
}
