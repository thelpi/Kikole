using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models.Requests
{
    public class NameProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Name;
    }
}
