using System;
using System.Collections.Generic;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
{
    public static class ProposalChart
    {
        public static int BasePoints => 1000;
        public static int SubmissionBasePoints => 500;
        public static int SubmissionBonusPoints => 1000;
        public static int SubmissionLosePointsByLeader => 100;
        public static int SubmissionThresholdlosePoints => 750;

        public static readonly IReadOnlyDictionary<ProposalTypes, (int points, bool isRate)> ProposalTypesCost
            = new Dictionary<ProposalTypes, (int, bool)>
            {
                { ProposalTypes.Club, (50, false) },
                { ProposalTypes.Country, (25, false) },
                { ProposalTypes.Name, (400, false) },
                { ProposalTypes.Position, (75, false) },
                { ProposalTypes.Year, (25, false) },
                { ProposalTypes.Clue, (50, true) },
                { ProposalTypes.Leaderboard, (25, false) },
                { ProposalTypes.Continent, (100, false) }
            };

        public static DateTime FirstDate = new DateTime(2022, 03, 05).Date;

        public static DateTime HiddenDate => FirstDate.AddDays(-1);

#if DEBUG
        public static DateTime ContinentValuatedStart = new DateTime(2022, 10, 30).Date;
#else
        public static DateTime ContinentValuatedStart = new DateTime(2022, 11, 01).Date;
#endif
        public static DateTime FirstMonth => new DateTime(FirstDate.Year, FirstDate.Month, 1);

        public static int SubmissionMaxPoints => SubmissionBasePoints + SubmissionBonusPoints;
    }
}
