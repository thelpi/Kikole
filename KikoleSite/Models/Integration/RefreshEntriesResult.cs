using System.Collections.Generic;

namespace KikoleSite.Elite.Models.Integration
{
    public class RefreshEntriesResult
    {
        public int ReplacedEntriesCount { get; set; }
        public IReadOnlyCollection<string> Errors { get; set; }
    }
}
