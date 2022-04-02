using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class YearProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Year;

        internal override string GetTip(PlayerDto player)
        {
            return ushort.Parse(Value) > player.YearOfBirth
                ? SPA.TextResources.TipOlderPlayer
                : SPA.TextResources.TipYoungerPlayer;
        }

        internal override string IsValid()
        {
            if (Value == null)
                return SPA.TextResources.InvalidValue;

            if (!ushort.TryParse(Value, out _))
                return SPA.TextResources.InvalidValue;

            return null;
        }
    }
}
