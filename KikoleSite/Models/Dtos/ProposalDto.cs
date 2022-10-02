using System;

namespace KikoleSite.Models.Dtos
{
    public class ProposalDto : BaseDto
    {
        public ulong UserId { get; set; }

        public ulong ProposalTypeId { get; set; }

        public string Value { get; set; }

        public byte Successful { get; set; }

        public DateTime ProposalDate { get; set; }

        public string Ip { get; set; }

        internal bool IsCurrentDay => ProposalDate == CreationDate.Date;
    }
}
