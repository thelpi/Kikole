using System;

namespace KikoleSite.Api
{
    public class Leader
    {
        public ulong UserId { get; set; }

        public int Position { get; set; }

        public string Login { get; set; }

        public int TotalPoints { get; set; }

        public TimeSpan BestTime { get; set; }

        public int SuccessCount { get; set; }
    }
}
