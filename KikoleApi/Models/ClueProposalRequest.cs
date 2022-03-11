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
            bool? successful = null;
            var value = ProposalResponse.GetValueFromProposalType(ProposalType,
                Value, ref successful, player, playerClubs, clubs);

            return new ProposalResponse
            {
                Successful = successful.Value,
                Value = value,
                TotalPoints = SourcePoints,
                LostPoints = ProposalChart.Default.ProposalTypesCost[ProposalType],
                ProposalType = ProposalType
            };
        }

        internal override string IsValid()
        {
            return null;
        }
    }
}
