namespace KikoleApi.Models.Requests
{
    public class PlayerSubmissionValidationRequest
    {
        public ulong PlayerId { get; set; }

        public bool IsAccepted { get; set; }

        public string ClueEdit { get; set; }

        public string RefusalReason { get; set; }

        internal string IsValid(TextResources resources)
        {
            if (PlayerId == 0)
                return resources.InvalidPlayerId;

            if (!IsAccepted && string.IsNullOrWhiteSpace(RefusalReason))
                return resources.RefusalWithoutReason;

            return null;
        }
    }
}
