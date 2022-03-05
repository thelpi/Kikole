using System.Collections.Generic;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class NameProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Name;

        internal override void CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            Successful = player.AllowedNames.Contains(Value.Sanitize());
            SuccessfulValue = Successful ? player.Name : null;
        }
    }
}
