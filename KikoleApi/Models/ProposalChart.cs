using KikoleApi.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleApi.Models
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

        internal readonly IReadOnlyDictionary<ProposalTypes, int> ProposalTypesCost
            = new Dictionary<ProposalTypes, int>
            {
                { ProposalTypes.Club, 50 },
                { ProposalTypes.Country, 25 },
                { ProposalTypes.Name, 400 },
                { ProposalTypes.Position, 75 },
                { ProposalTypes.Year, 25 },
                { ProposalTypes.Clue, 400 }
            };

        public IReadOnlyDictionary<string, int> ProposalTypesCostString
            => ProposalTypesCost.ToDictionary(x =>x.Key.ToString(), x => x.Value);

        public DateTime FirstDate { get; internal set; }
    }
}
