using System;
using System.Collections.Generic;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
{
    public static class ProposalChart
    {
        public static readonly int BasePoints = 1000;
        public static readonly int SubmissionBasePoints = 500;
        public static readonly int SubmissionBonusPoints = 1000;
        public static readonly int SubmissionLosePointsByLeader = 100;
        public static readonly int SubmissionThresholdlosePoints = 750;
        public static readonly int SubmissionPoints = 1000;

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
                { ProposalTypes.Continent, (100, false) },
                { ProposalTypes.Federation, (100, false) }
            };

        public static readonly DateTime FirstDate = new DateTime(2022, 03, 05).Date;

        public static readonly DateTime HiddenDate = FirstDate.AddDays(-1);

#if DEBUG
        public static readonly DateTime ContinentValuatedStart = new DateTime(2022, 10, 30).Date;
#else
        public static readonly DateTime ContinentValuatedStart = new DateTime(2022, 11, 01).Date;
#endif

#if DEBUG
        public static readonly DateTime FederationsValuatedStart = new DateTime(2022, 11, 06).Date;
#else
        public static readonly DateTime FederationsValuatedStart = new DateTime(2099, 11, 01).Date;
#endif

#if DEBUG
        public static readonly DateTime SubmissionNewChartStart = FirstDate;
#else
        public static readonly DateTime SubmissionNewChartStart = new DateTime(2022, 11, 04).Date;
#endif

        public static readonly DateTime FirstMonth = new DateTime(FirstDate.Year, FirstDate.Month, 1);

        public static readonly int SubmissionMaxPoints = SubmissionBasePoints + SubmissionBonusPoints;
    }
}
