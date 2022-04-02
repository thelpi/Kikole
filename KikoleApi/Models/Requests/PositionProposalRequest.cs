using System;
using System.Linq;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class PositionProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Position;

        internal override string IsValid()
        {
            if (Value == null)
                return SPA.TextResources.InvalidValue;

            if (int.TryParse(Value, out var positionId))
            {
                if (!Enum.GetValues(typeof(Positions)).Cast<int>().Contains(positionId))
                    return SPA.TextResources.InvalidValue;
            }
            else
            {
                if (!Enum.IsDefined(typeof(Positions), Value))
                    return SPA.TextResources.InvalidValue;
            }

            return null;
        }
    }
}
