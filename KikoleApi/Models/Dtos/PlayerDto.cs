﻿using System;

namespace KikoleApi.Models.Dtos
{
    public class PlayerDto : BaseDto
    {
        public string Name { get; set; }

        public string AllowedNames { get; set; }

        public ushort YearOfBirth { get; set; }

        public ulong CountryId { get; set; }

        public DateTime? ProposalDate { get; set; }

        public string Clue { get; set; }

        public ulong? BadgeId { get; set; }

        public ulong PositionId { get; set; }

        public ulong CreationUserId { get; set; }
    }
}
