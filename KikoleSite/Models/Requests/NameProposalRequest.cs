using KikoleSite.Models.Enums;

namespace KikoleSite.Models.Requests
{
    public class NameProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Name;
    }
}
