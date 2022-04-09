using System;
using System.Collections.Generic;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models
{
    public class ProposalChart
    {
        private static ProposalChart _default;

        public static ProposalChart Default => _default ?? (_default = new ProposalChart());

        private ProposalChart() { }

        public int BasePoints => 1000;

        public int SubmissionBasePoints => 500;

        public int SubmissionBonusPoints => 1000;

        public int SubmissionLosePointsByLeader => 100;

        public int SubmissionThresholdlosePoints => 750;

        public IReadOnlyDictionary<ProposalTypes, int> ProposalTypesCost
            => new Dictionary<ProposalTypes, int>
            {
                { ProposalTypes.Club, 50 },
                { ProposalTypes.Country, 25 },
                { ProposalTypes.Name, 400 },
                { ProposalTypes.Position, 75 },
                { ProposalTypes.Year, 25 },
                { ProposalTypes.Clue, 250 }
            };
        
        public DateTime FirstDate { get; internal set; }
    }
}
