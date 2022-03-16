using System;
using System.Collections.Generic;

namespace KikoleApi.Models
{
    public class ProposalChart
    {
        private static ProposalChart _default;

        public static ProposalChart Default => _default ?? (_default = new ProposalChart());

        private ProposalChart() { }

        public int BasePoints = 1000;

        public IReadOnlyDictionary<ProposalType, int> ProposalTypesCost
            = new Dictionary<ProposalType, int>
            {
                { ProposalType.Club, 50 },
                { ProposalType.Country, 25 },
                { ProposalType.Name, 400 },
                { ProposalType.Position, 200 },
                { ProposalType.Year, 25 },
            };

        // TODO: binds to database (if possible)
        public DateTime FirstDate = new DateTime(2022, 03, 05);
    }
}
