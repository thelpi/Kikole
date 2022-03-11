using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class NameProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Name;

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
