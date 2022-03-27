using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class ClubProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Club;
    }
}
