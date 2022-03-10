using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ClueProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Clue;

        internal override int PointsCost => 100;

        internal override ProposalResponse CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            return new ProposalResponse
            {
                Successful = true,
                Value = player.Clue,
                TotalPoints = SourcePoints,
                LostPoints = PointsCost
            };
        }

        internal override string IsValid()
        {
            return null;
        }
    }
}
