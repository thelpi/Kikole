using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class PlayerSubmissionValidationRequest
    {
        public ulong PlayerId { get; set; }

        public bool IsAccepted { get; set; }

        public IReadOnlyDictionary<string, string> ClueEditLanguages { get; set; }

        public IReadOnlyDictionary<string, string> EasyClueEditLanguages { get; set; }

        public string ClueEditEn { get; set; }

        public string EasyClueEditEn { get; set; }

        public string RefusalReason { get; set; }
    }
}
