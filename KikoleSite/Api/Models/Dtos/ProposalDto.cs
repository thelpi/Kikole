using System;

namespace KikoleSite.Api.Models.Dtos
{
    public class ProposalDto : BaseDto
    {
        public ulong UserId { get; set; }

        public ulong ProposalTypeId { get; set; }

        public string Value { get; set; }

        public byte Successful { get; set; }

        public DateTime ProposalDate { get; set; }

        internal bool IsCurrentDay => ProposalDate == CreationDate.Date;
    }
}
