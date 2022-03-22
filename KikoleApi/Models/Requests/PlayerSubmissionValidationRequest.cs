namespace KikoleApi.Models.Requests
{
    public class PlayerSubmissionValidationRequest
    {
        public ulong PlayerId { get; set; }

        public bool IsAccepted { get; set; }

        public string ClueEdit { get; set; }

        public string RefusalReason { get; set; }

        internal bool IsValid()
        {
            return PlayerId > 0
                && (IsAccepted
                    || (!IsAccepted
                        && !string.IsNullOrWhiteSpace(RefusalReason)));
        }
    }
}
