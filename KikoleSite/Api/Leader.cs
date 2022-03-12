using System;

namespace KikoleSite.Api
{
    public class Leader
    {
        public int Position { get; set; }

        public string Login { get; set; }

        public uint TotalPoints { get; set; }

        public TimeSpan BestTime { get; set; }

        public int SuccessCount { get; set; }
    }
}
