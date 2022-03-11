using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class PositionProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Position;

        internal override int PointsCost => 200;

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
            var success = player.PositionId == ulong.Parse(Value);
            return new ProposalResponse
            {
                Successful = success,
                LostPoints = success ? 0 : PointsCost,
                TotalPoints = SourcePoints,
                Value = success
                    ? player.PositionId
                    : default(ulong?)
            };
        }
    }
}
