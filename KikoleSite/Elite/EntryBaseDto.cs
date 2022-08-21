using System;

namespace KikoleSite.Elite
{
    public class EntryBaseDto
    {
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public long Time { get; set; }
        public DateTime? Date { get; set; }
        public Engine Engine { get; set; }
    }
}
