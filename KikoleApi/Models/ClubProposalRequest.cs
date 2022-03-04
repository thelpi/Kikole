using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ClubProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Club;

        internal override string IsSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            var ok = clubs.FirstOrDefault(c => c.AllowedNames.Contains(Value.Sanitize()));
            return ok?.Name;
        }
    }
}
