using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ClueProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Clue;

        internal override ProposalResponse CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            return new ProposalResponse
            {
                Successful = true,
                Value = player.Clue
            };
        }

        internal override string IsValid()
        {
            return null;
        }
    }
}
