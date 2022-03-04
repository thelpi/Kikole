using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ClubProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Club;

        internal override bool IsSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            return clubs.Any(c => c.AllowedNames.Contains(Value.Sanitize()));
        }
    }
}
