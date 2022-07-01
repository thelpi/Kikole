using System;

namespace KikoleSite.Api.Models
{
    public class LeaderboardItem
    {
        public int Rank { get; set; }
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public int Points { get; set; }
        public TimeSpan BestTime { get; set; }
        public int KikolesFound { get; set; }
        public int KikolesAttempted { get; set; }
        public int KikolesProposed { get; set; }
    }
}
