using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests
{
    public class YearProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Year;

        internal override string GetTip(PlayerDto player, IStringLocalizer resources)
        {
            return ushort.Parse(Value) > player.YearOfBirth
                ? resources["TipOlderPlayer"]
                : resources["TipYoungerPlayer"];
        }

        internal override string IsValid(IStringLocalizer resources)
        {
            if (Value == null)
                return resources["InvalidValue"];

            if (!ushort.TryParse(Value, out _))
                return resources["InvalidValue"];

            return null;
        }
    }
}
