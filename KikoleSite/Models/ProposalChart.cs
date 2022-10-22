using System;
using System.Collections.Generic;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
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

        public readonly IReadOnlyDictionary<ProposalTypes, (int points, bool isRate)> ProposalTypesCost
            = new Dictionary<ProposalTypes, (int, bool)>
            {
                { ProposalTypes.Club, (50, false) },
                { ProposalTypes.Country, (25, false) },
                { ProposalTypes.Name, (400, false) },
                { ProposalTypes.Position, (75, false) },
                { ProposalTypes.Year, (25, false) },
                { ProposalTypes.Clue, (50, true) },
                { ProposalTypes.Leaderboard, (25, false) }
            };

        public DateTime FirstDate { get; internal set; }

        public int SubmissionMaxPoints => SubmissionBasePoints + SubmissionBonusPoints;
    }
}
