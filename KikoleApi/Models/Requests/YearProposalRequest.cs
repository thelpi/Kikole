using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class YearProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Year;

        internal override string GetTip(PlayerDto player, TextResources resources)
        {
            return ushort.Parse(Value) > player.YearOfBirth
                ? resources.TipOlderPlayer
                : resources.TipYoungerPlayer;
        }

        internal override string IsValid(TextResources resources)
        {
            if (Value == null)
                return resources.InvalidValue;

            if (!ushort.TryParse(Value, out _))
                return resources.InvalidValue;

            return null;
        }
    }
}
