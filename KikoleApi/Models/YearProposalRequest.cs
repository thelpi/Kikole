using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class YearProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Year;

        internal override bool IsSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            return ushort.Parse(Value) == player.YearOfBirth;
        }

        internal override string IsValid()
        {
            if (!ushort.TryParse(Value, out _))
                return "Invalid value";

            return null;
        }
    }
}
