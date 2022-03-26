using System;
using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class ProposalChart
    {
        public DateTime FirstDate { get; set; }

        public int BasePoints { get; set; }

        public int ChallengeWithdrawalPoints = 1000;

        public IReadOnlyDictionary<ProposalType, int> ProposalTypesCost { get; set; }
    }
}
