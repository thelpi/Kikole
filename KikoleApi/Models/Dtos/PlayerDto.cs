using System;

namespace KikoleApi.Models.Dtos
{
    public class PlayerDto : BaseDto
    {
        public string Name { get; set; }

        public string AllowedNames { get; set; }

        public ushort YearOfBirth { get; set; }

        public ulong Country1Id { get; set; }

        public ulong? Country2Id { get; set; }

        public DateTime? ProposalDate { get; set; }
    }
}
