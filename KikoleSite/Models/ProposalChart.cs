using System;
using System.Collections.Generic;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
{
    public static class ProposalChart
    {
        public static readonly int BasePoints = 1000;
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
                { ProposalTypes.Continent, (100, false) }
            };

        // TODO: provisoire, a sortir en configuration ou a deduire du MIN(proposal_date) en base
        public static readonly DateTime FirstDate = new DateTime(2026, 09, 02).Date;

        public static readonly DateTime HiddenDate = FirstDate.AddDays(-1);

        public static readonly DateTime FirstMonth = new DateTime(FirstDate.Year, FirstDate.Month, 1);
    }
}
