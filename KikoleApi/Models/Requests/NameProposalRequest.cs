using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class NameProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Name;
    }
}
