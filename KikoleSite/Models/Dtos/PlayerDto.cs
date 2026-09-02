using System;

namespace KikoleSite.Models.Dtos
{
    public class PlayerDto : BaseDto
    {
        public string Name { get; set; } = null!;

        public string AllowedNames { get; set; } = null!;

        public ushort YearOfBirth { get; set; }

        public ulong ContinentId { get; set; }

        public ulong CountryId { get; set; }

        public DateTime? ProposalDate { get; set; }

        public string Clue { get; set; } = null!;

        public string EasyClue { get; set; } = null!;

        public ulong? BadgeId { get; set; }

        public ulong PositionId { get; set; }

        public ulong CreationUserId { get; set; }

        public DateTime? RejectDate { get; set; }

        public byte HideCreator { get; set; }
    }
}
