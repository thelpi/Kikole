using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models.Requests
{
    public class NameProposalRequest : BaseProposalRequest
    {
        public NameProposalRequest(IClock clock)
            : base(clock) { }

        internal override ProposalTypes ProposalType => ProposalTypes.Name;
    }
}
