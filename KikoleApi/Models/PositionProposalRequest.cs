using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class PositionProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Position;

        internal override string IsValid()
        {
            if (int.TryParse(Value, out var positionId))
            {
                if (!Enum.GetValues(typeof(Position)).Cast<int>().Contains(positionId))
                    return "Invalid value";
            }
            else
            {
                if (!Enum.IsDefined(typeof(Position), Value))
                    return "Invalid value";
            }

            return null;
        }

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
                LostPoints = success.Value
                    ? 0
                    : ProposalChart.Default.ProposalTypesCost[ProposalType],
                TotalPoints = SourcePoints,
                Value = value,
                ProposalType = ProposalType
            };
        }
    }
}
