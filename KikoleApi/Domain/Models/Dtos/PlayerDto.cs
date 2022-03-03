using System;

namespace KikoleApi.Domain.Models.Dtos
{
    internal class PlayerDto
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public uint YearOfBirth { get; set; }

        public ulong Country1Id { get; set; }

        public ulong? Country2Id { get; set; }

        public DateTime? ProposalDate { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime UpdateDate { get; set; }
    }
}
