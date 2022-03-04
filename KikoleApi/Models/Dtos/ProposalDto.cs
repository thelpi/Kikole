using System;

namespace KikoleApi.Models.Dtos
{
    public class ProposalDto
    {
        public ulong Id { get; set; }

        public ulong UserId { get; set; }

        public ulong ProposalTypeId { get; set; }

        public string Value { get; set; }

        public byte Successful { get; set; }

        public DateTime ProposalDate { get; set; }
    }
}
