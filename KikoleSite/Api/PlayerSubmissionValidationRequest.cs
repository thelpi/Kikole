namespace KikoleSite.Api
{
    public class PlayerSubmissionValidationRequest
    {
        public ulong PlayerId { get; set; }

        public bool IsAccepted { get; set; }

        public string ClueEdit { get; set; }

        public string RefusalReason { get; set; }
    }
}
