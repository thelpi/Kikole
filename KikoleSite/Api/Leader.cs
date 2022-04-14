using System;

namespace KikoleSite.Api
{
    public class Leader
    {
        public ulong UserId { get; set; }

        public int Position { get; set; }

        public string Login { get; set; }

        public int TotalPoints { get; set; }

        public TimeSpan BestTime => new TimeSpan(0, 0, BestTimeSec);

        public int BestTimeSec { get; set; }

        public int SuccessCount { get; set; }
    }
}
