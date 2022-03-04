using System.Collections.Generic;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class NameProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Name;

        internal override string IsSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            var ok = player.AllowedNames.Contains(Value.Sanitize());
            return ok
                ? player.Name
                : null;
        }
    }
}
