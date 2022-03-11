using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ClubProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Club;

        internal override ProposalResponse CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            bool? success = null;
            var value = ProposalResponse.GetValueFromProposalType(ProposalType,
                Value, ref success, player, playerClubs, clubs);

            return new ProposalResponse
            {
                Successful = success.Value,
                Value = value,
                TotalPoints = SourcePoints,
                LostPoints = success.Value
                    ? 0
                    : ProposalChart.Default.ProposalTypesCost[ProposalType],
                ProposalType = ProposalType
            };
        }
    }
}
