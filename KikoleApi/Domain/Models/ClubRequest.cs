using System.Collections.Generic;

namespace KikoleApi.Domain.Models
{
    public class ClubRequest
    {
        public string Name { get; set; }

        public byte HistoryPosition { get; set; }

        public byte ImportancePosition { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }

        internal bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name)
                && HistoryPosition > 0
                && ImportancePosition > 0
                && AllowedNames.IsValid();
        }
    }
}
