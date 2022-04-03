using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class PlayerSubmissionValidationRequest
    {
        public ulong PlayerId { get; set; }

        public bool IsAccepted { get; set; }

        public IReadOnlyDictionary<Languages, string> ClueEditLangugages { get; set; }

        public string ClueEditEn { get; set; }

        public string RefusalReason { get; set; }
    }
}
