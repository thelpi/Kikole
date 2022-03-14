using System;

namespace KikoleApi.Models.Dtos
{
    public class LeaderDto : BaseDto
    {
        public ulong UserId { get; set; }

        public DateTime ProposalDate { get; set; }

        public ushort Points { get; set; }

        public ushort Time { get; set; }
    }
}
