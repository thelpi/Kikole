using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models.Requests
{
    public class ClubProposalRequest : BaseProposalRequest
    {
        public ClubProposalRequest(IClock clock)
            : base(clock) { }

        internal override ProposalTypes ProposalType => ProposalTypes.Club;
    }
}
