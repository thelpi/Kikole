using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class ClueProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Club;

        internal override string IsValid(TextResources resources)
        {
            return null;
        }

        internal override string GetTip(PlayerDto player, TextResources resources)
        {
            return resources.ClueAvailable;
        }
    }
}
