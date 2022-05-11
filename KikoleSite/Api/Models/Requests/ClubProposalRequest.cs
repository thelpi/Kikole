using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models.Requests
{
    public class ClubProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Club;
    }
}
