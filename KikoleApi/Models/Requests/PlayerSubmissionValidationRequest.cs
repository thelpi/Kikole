namespace KikoleApi.Models.Requests
{
    public class PlayerSubmissionValidationRequest
    {
        public ulong PlayerId { get; set; }

        public bool IsAccepted { get; set; }

        public string ClueEdit { get; set; }

        public string RefusalReason { get; set; }

        internal string IsValid()
        {
            if (PlayerId == 0)
                return SPA.TextResources.InvalidPlayerId;

            if (!IsAccepted && string.IsNullOrWhiteSpace(RefusalReason))
                return SPA.TextResources.RefusalWithoutReason;

            return null;
        }
    }
}
