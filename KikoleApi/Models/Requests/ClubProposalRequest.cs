namespace KikoleApi.Models.Requests
{
    public class ClubProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Club;
    }
}
