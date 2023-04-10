using System.Collections.Generic;

namespace KikoleSite.ViewDatas
{
    public class PlayerWorldRecordsItemData
    {
        public IReadOnlyCollection<StandingItemData> UntiedWorldRecords { get; set; }
        public IReadOnlyCollection<StandingItemData> WorldRecords { get; set; }
        public IReadOnlyCollection<StandingItemData> UntiedSlayWorldRecords { get; set; }
        public IReadOnlyCollection<StandingItemData> SlayWorldRecords { get; set; }
    }
}
