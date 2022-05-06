using System;

namespace KikoleSite.Api.Models.Dtos
{
    public class LeaderDto : BaseDto
    {
        internal const int FirstDayMinutes = 1440;

        public ulong UserId { get; set; }

        public DateTime ProposalDate { get; set; }

        public ushort Points { get; set; }

        public int Time { get; set; }

        internal bool IsCurrentDay => ProposalDate.Date == CreationDate.Date;
    }
}
