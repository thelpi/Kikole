using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
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

        internal string IsValid(TextResources resources)
        {
            if (PlayerId == 0)
                return resources.InvalidPlayerId;

            if (!IsAccepted && string.IsNullOrWhiteSpace(RefusalReason))
                return resources.RefusalWithoutReason;

            if (IsAccepted)
            {
                if (ClueEditLanguages?.ContainsKey(Languages.fr) != true
                    || ClueEditLanguages.Values.Any(cel => string.IsNullOrWhiteSpace(cel)))
                    return resources.InvalidClue;

                if (EasyClueEditLanguages?.ContainsKey(Languages.fr) != true
                    || EasyClueEditLanguages.Values.Any(cel => string.IsNullOrWhiteSpace(cel)))
                    return resources.InvalidClue;
            }

            return null;
        }
    }
}
