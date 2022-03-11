using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class YearProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Year;

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
                Tip = $"The player is {(ushort.Parse(Value) > player.YearOfBirth ? "older" : "younger")}",
                TotalPoints = SourcePoints,
                LostPoints = success.Value
                    ? 0
                    : ProposalChart.Default.ProposalTypesCost[ProposalType],
                ProposalType = ProposalType
            };
        }

        internal override string IsValid()
        {
            if (!ushort.TryParse(Value, out _))
                return "Invalid value";

            return null;
        }
    }
}
