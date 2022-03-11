using System;
using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class ProposalChart
    {
        public DateTime FirstDate { get; set; }

        public int BasePoints { get; set; }

        public IReadOnlyDictionary<ProposalType, int> ProposalTypesCost { get; set; }
    }
}
