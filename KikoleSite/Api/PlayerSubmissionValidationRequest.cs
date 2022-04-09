using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class PlayerSubmissionValidationRequest
    {
        public ulong PlayerId { get; set; }

        public bool IsAccepted { get; set; }

        public IReadOnlyDictionary<Languages, string> ClueEditLanguages { get; set; }

        public IReadOnlyDictionary<Languages, string> EasyClueEditLanguages { get; set; }

        public string ClueEditEn { get; set; }

        public string EasyClueEditEn { get; set; }

        public string RefusalReason { get; set; }
    }
}
