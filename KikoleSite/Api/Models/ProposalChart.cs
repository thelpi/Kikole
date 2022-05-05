using System;
using System.Collections.Generic;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models
{
    public class ProposalChart
    {
        private static ProposalChart _default;

        public static ProposalChart Default => _default ??= new ProposalChart();

        private ProposalChart() { }

        public int BasePoints => 1000;

        public int SubmissionBasePoints => 500;

        public int SubmissionBonusPoints => 1000;

        public int SubmissionLosePointsByLeader => 100;

        public int SubmissionThresholdlosePoints => 750;

        public readonly IReadOnlyDictionary<ProposalTypes, int> ProposalTypesCost
            = new Dictionary<ProposalTypes, int>
            {
                { ProposalTypes.Club, 50 },
                { ProposalTypes.Country, 25 },
                { ProposalTypes.Name, 400 },
                { ProposalTypes.Position, 75 },
                { ProposalTypes.Year, 25 },
                { ProposalTypes.Clue, 400 }
            };

        public DateTime FirstDate { get; internal set; }

        public int SubmissionMaxPoints => SubmissionBasePoints + SubmissionBonusPoints;
    }
}
