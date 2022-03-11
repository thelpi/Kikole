namespace KikoleApi.Models.Requests
{
    public class NameProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Name;
    }
}
