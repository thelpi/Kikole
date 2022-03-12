using System;

namespace KikoleApi.Models.Dtos
{
    public class ProposalDto : BaseDto
    {
        public ulong? UserId { get; set; }

        public string Ip { get; set; }

        public ulong ProposalTypeId { get; set; }

        public uint DaysBefore { get; set; }

        public string Value { get; set; }

        public byte Successful { get; set; }

        public DateTime ProposalDate { get; set; }
    }
}
