namespace KikoleApi.Models
{
    public class ProposalResponse
    {
        public bool Successful { get; }

        public object Value { get; }

        internal ProposalResponse(BaseProposalRequest request)
        {
            Successful = request.Successful;
            Value = request.SuccessfulValue;
        }
    }
}
