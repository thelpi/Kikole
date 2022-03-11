namespace KikoleApi.Models.Requests
{
    public class ClueProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Clue;

        internal override string IsValid()
        {
            return null;
        }
    }
}
