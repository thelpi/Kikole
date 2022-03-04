using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class YearProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Year;

        internal override string IsSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            var ok = ushort.Parse(Value) == player.YearOfBirth;
            return ok
                ? player.YearOfBirth.ToString()
                : null;
        }

        internal override string IsValid()
        {
            if (!ushort.TryParse(Value, out _))
                return "Invalid value";

            return null;
        }
    }
}
