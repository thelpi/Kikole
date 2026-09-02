using System;

namespace KikoleSite.Models.Dtos
{
    public class PlayerDto : BaseDto
    {
        public required string Name { get; set; }

        public required string AllowedNames { get; set; }

        public ushort YearOfBirth { get; set; }

        public ulong ContinentId { get; set; }

        public ulong CountryId { get; set; }

        public DateTime? ProposalDate { get; set; }

        public required string Clue { get; set; }

        public required string EasyClue { get; set; }

        public ulong? BadgeId { get; set; }

        public ulong PositionId { get; set; }

        public ulong CreationUserId { get; set; }

        public DateTime? RejectDate { get; set; }

        public byte HideCreator { get; set; }
    }
}
