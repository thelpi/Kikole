using System;
using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public abstract class BaseProposalRequest
    {
        public DateTime ProposalDate { get; set; }

        public string Value { get; set; }

        internal abstract ProposalType ProposalType { get; }

        internal protected bool Successful { get; protected set; }

        internal protected object SuccessfulValue { get; protected set; }

        internal abstract void CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs);

        internal virtual string IsValid()
        {
            if (string.IsNullOrWhiteSpace(Value))
                return "Invalid value";

            return null;
        }

        internal virtual ProposalDto ToDto(ulong userId)
        {
            return new ProposalDto
            {
                ProposalDate = ProposalDate,
                Successful = (byte)(Successful ? 1 : 0),
                UserId = userId,
                Value = Value.ToString(),
                ProposalTypeId = (ulong)ProposalType
            };
        }
    }
}
