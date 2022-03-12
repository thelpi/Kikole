using System;

namespace KikoleApi.Models.Dtos
{
    public class LeaderboardDto : BaseDto
    {
        public ulong? UserId { get; set; }

        public string Ip { get; set; }

        public DateTime ProposalDate { get; set; }

        public ushort Points { get; set; }

        public ushort Time { get; set; }

        internal (string, bool) Key => UserId > 0
            ? (UserId.ToString(), false)
            : (Ip, true);
    }
}
