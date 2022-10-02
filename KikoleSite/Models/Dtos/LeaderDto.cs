using System;

namespace KikoleSite.Models.Dtos
{
    public class LeaderDto : BaseDto
    {
        public ulong UserId { get; set; }

        public DateTime ProposalDate { get; set; }

        public ushort Points { get; set; }

        public int Time { get; set; }

        internal bool IsCurrentDay => ProposalDate.Date == CreationDate.Date;
    }
}
