using System.Collections.Generic;
using KikoleSite.Api;

namespace KikoleSite.Models
{
    public class ChallengeModel
    {
        public string ErrorMessage { get; set; }

        public string InfoMessage { get; set; }

        public Challenge AcceptedChallenge { get; set; }

        public Challenge TodayChallenge { get; set; }

        public IReadOnlyCollection<Challenge> RequestedChallenges { get; set; }

        public IReadOnlyCollection<Challenge> WaitingForResponseChallenges { get; set; }

        public IReadOnlyCollection<User> Users { get; set; }

        public ulong SelectedChallengeId { get; set; }

        public bool AcceptChallenge { get; set; }

        public ulong SelectedUserId { get; set; }

        public byte PointsRate { get; set; }

        public IReadOnlyCollection<Challenge> ChallengesHistory { get; set; }
    }
}
