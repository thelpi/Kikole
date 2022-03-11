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
            var numValue = ushort.Parse(Value);
            var success = numValue == player.YearOfBirth;
            return new ProposalResponse
            {
                Successful = success,
                Value = success
                    ? player.YearOfBirth.ToString()
                    : null,
                Tip = $"The player is {(numValue > player.YearOfBirth ? "older" : "younger")}",
                TotalPoints = SourcePoints,
                LostPoints = success ? 0 : ProposalChart.Default.ProposalTypesCost[ProposalType]
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
