using System;
using KikoleSite.Enums;

namespace KikoleSite.Dtos
{
    public class EntryBaseDto
    {
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public int Time { get; set; }
        public DateTime? Date { get; set; }
        public Engine Engine { get; set; }

        internal bool AreEqual(EntryBaseDto entry)
        {
            // BUG: manage "simulatedDate" from child class
            return AreSame(entry)
                && (
                    (!entry.Date.HasValue && !Date.HasValue)
                    || entry.Date == Date);
        }

        internal bool AreSame(EntryBaseDto entry)
        {
            return entry.Time == Time
                && entry.Level == Level
                && entry.Stage == Stage
                && entry.Engine == Engine;
        }
    }
}
