using System;
using System.Linq;

namespace KikoleApi.Models.Requests
{
    public class PositionProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Position;

        internal override string IsValid()
        {
            if (Value == null)
                return "Invalid value";

            if (int.TryParse(Value, out var positionId))
            {
                if (!Enum.GetValues(typeof(Positions)).Cast<int>().Contains(positionId))
                    return "Invalid value";
            }
            else
            {
                if (!Enum.IsDefined(typeof(Positions), Value))
                    return "Invalid value";
            }

            return null;
        }
    }
}
