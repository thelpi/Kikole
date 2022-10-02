using KikoleSite.Models.Enums;

namespace KikoleSite.Models.Requests
{
    public class ClubProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Club;
    }
}
