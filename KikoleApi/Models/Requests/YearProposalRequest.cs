using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class YearProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Year;

        internal override string GetTip(PlayerDto player)
        {
            return $"The player is {(ushort.Parse(Value) > player.YearOfBirth ? "older" : "younger")}";
        }

        internal override string IsValid()
        {
            if (Value == null)
                return "Invalid value";

            if (!ushort.TryParse(Value, out _))
                return "Invalid value";

            return null;
        }
    }
}
