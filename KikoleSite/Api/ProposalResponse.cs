using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class ProposalResponse
    {
        public bool Successful { get; set; }

        public string Value { get; set; }

        public string Tip { get; set; }

        public int LostPoints { get; set; }

        public int TotalPoints { get; set; }

        public ProposalType ProposalType { get; set; }

        public IReadOnlyCollection<UserBadge> CollectedBadges { get; set; }

        internal IReadOnlyCollection<PlayerClub> GetPlayerClubsValue()
        {
            return System.Text.Json.JsonSerializer.Deserialize<IReadOnlyCollection<PlayerClub>>(Value);
        }
    }
}
