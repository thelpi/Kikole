using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Api
{
    public class ProposalChart
    {
        public DateTime FirstDate { get; set; }

        public int BasePoints { get; set; }

        public int ChallengeWithdrawalPoints = 1000;

        public IReadOnlyDictionary<string, int> ProposalTypesCostString { get; set; }

        public int SubmissionBasePoints { get; set; }

        public int SubmissionBonusPoints { get; set; }

        public int SubmissionMaxPoints => SubmissionBasePoints + SubmissionBonusPoints;

        public int SubmissionLosePointsByLeader { get; set; }

        public int SubmissionThresholdlosePoints { get; set; }

        public int GetProposalTypesCost(ProposalType proposalType)
        {
            return ProposalTypesCostString[proposalType.ToString()];
        }
    }
}
