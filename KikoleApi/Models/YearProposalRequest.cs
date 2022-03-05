using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class YearProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Year;

        internal override void CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            Successful = ushort.Parse(Value) == player.YearOfBirth;
            SuccessfulValue = Successful
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
